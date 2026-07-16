# Kodx RPI — Contexto para IA

API .NET responsável por baixar RPIs (Revista da Propriedade Industrial) do site do INPI, validar consistência, converter para txt, salvar no Blob Storage (Azure) e persistir publicações individualizadas em Postgres, servindo o sistema Kodx e outros serviços consumidores.

- Especificação completa: `specs/Especificações - Kodx RPI.md`
- Código-fonte: `src/` (solução em `Kodx.Rpi.slnx`), testes em `tests/`

Antes de iniciar qualquer trabalho nesta base, leia este arquivo. Ao tomar decisões relevantes de arquitetura, fechar dúvidas em aberto ou avançar de fase, atualize-o.

## Status atual

Fase 2 implementada na branch `feat/app-skeleton`, aguardando validação para merge (squash) em `main`. Próximo passo após validação: fase 3 (modelagem do banco Postgres).

## Plano de implementação (por fases, validadas uma a uma)

1. ~~Scaffolding do repositório (docs de IA, git, estrutura de pastas)~~ — **concluído**
2. ~~Esqueleto da aplicação .NET (camadas DDD, Swagger, auth por API key, logging estruturado, Docker, CI básico)~~ — **implementado, em validação** (branch `feat/app-skeleton`)
3. Modelagem e migrations do banco Postgres (histórico de downloads/transformações, publicações em JSONB)
4. Download de RPIs do site do INPI
5. Conversão PDF → TXT
6. Armazenamento no Azure Blob Storage (seguindo estrutura/tags já existentes)
7. Extração e persistência das publicações individuais
8. Endpoints privados para o Kodx API (consulta de RPI, download de PDF, busca de publicações por edição)
9. Scripts de cronjob (Ubuntu 24.04) + documentação
10. CI/CD completo (sec scan, testes, build, deploy em versão fechada)

## Decisões tomadas

- Repositório GitHub: `git@github.com:marcosiw/kodx-rpi.git` (branch padrão `main`).
- Docs para IA ficam em `ai/`, com `ai/context.md` cumprindo o papel de guia principal (equivalente a um CLAUDE.md).
- `specs/` é versionado junto ao código (fonte de verdade da especificação).
- Fluxo de branches: cada fase do plano é implementada em uma branch própria a partir de `main`; ao finalizar, o merge em `main` é feito via **squash** (histórico enxuto na main, detalhado preservado na branch de origem).
- Esqueleto da aplicação (.NET 10, target `net10.0`):
  - Solução `Kodx.Rpi.slnx` na raiz; projetos de camada em `src/` (`Kodx.Rpi.Domain`, `Kodx.Rpi.Application`, `Kodx.Rpi.Infrastructure`, `Kodx.Rpi.Api`), testes em `tests/` (`Kodx.Rpi.Application.Tests`, `Kodx.Rpi.Infrastructure.Tests`, xUnit). Referências seguem DDD: Domain ← Application ← Infrastructure/Api.
  - Logging estruturado com Serilog (console em JSON via `CompactJsonFormatter`) + `UseSerilogRequestLogging()` para log de entrada/saída de requests.
  - Auth por API key: middleware próprio (`Kodx.Rpi.Api/Security/ApiKeyMiddleware.cs`), header configurável (default `X-Api-Key`), valor lido de config/env (`ApiKey__Value`, nunca commitado). Endpoints `/health`, `/swagger` e `/favicon.ico` isentos (o favicon é isento porque o navegador o busca automaticamente ao abrir qualquer página, o que gerava um 401 de ruído no log mesmo com a página principal respondendo certo).
  - Timeout default de 30s via `AddRequestTimeouts`/`UseRequestTimeouts` (built-in do ASP.NET Core), pode ser sobrescrito por endpoint com o atributo `[RequestTimeout]` quando necessário.
  - Swagger via Swashbuckle (`AddSwaggerGen`/`UseSwagger`/`UseSwaggerUI`), habilitado em Development. Optou-se por Swashbuckle em vez do `Microsoft.AspNetCore.OpenApi` nativo do template porque este último trouxe uma dependência transitiva (`Microsoft.OpenApi` 2.0.0) com vulnerabilidade conhecida (NU1903) na versão do pacote puxada pelo template.
  - Docker multi-stage (`Dockerfile` na raiz, `.dockerignore`), imagem final expõe porta 8080.
  - CI (`.github/workflows/ci.yml`): jobs `security` (dotnet list package --vulnerable) → `test` (dotnet test) → `build` → `deploy` (só dispara em tags `v*.*.*`; hoje só builda a imagem Docker, **push/deploy real para o Linode ainda não implementado** — depende de decisões de infra/registry em fase futura).
  - direnv: `.envrc.example` versionado como template; `.envrc` real ignorado pelo git.
  - Config local na IDE (Visual Studio/Rider): **um único mecanismo de config (variáveis de ambiente)** em todos os contextos — shell (`.envrc`/direnv), Docker/produção (env do orquestrador) e IDE. Para a IDE, que não herda o shell do direnv ao ser aberta direto (ex: ícone do Windows), usamos `src/Kodx.Rpi.Api/Properties/launchSettings.json`, que o Visual Studio/Rider já leem nativamente para variáveis de ambiente ao rodar o profile — **esse arquivo saiu do git** (contém valores locais como `ApiKey__Value`) e um `launchSettings.json.example` (com valores vazios) é o template versionado, no mesmo padrão do `.envrc.example`.
    - Avaliamos e descartamos `dotnet user-secrets`: ele só carrega em `Development` a partir de um arquivo fora do repo (`~/.microsoft/usersecrets/<GUID>/secrets.json`), nunca é publicado/empacotado e **não existe dentro de containers Docker** — seria uma segunda fonte de verdade desconectada de Docker/produção, o que o usuário preferiu evitar.

## Perguntas em aberto

- Existe outro repositório/serviço Kodx já em produção com convenções de Blob Storage (estrutura de pastas/tags) ou parsing de publicações que devemos seguir/portar?
- Há amostras de PDF/TXT de RPI disponíveis para desenvolver e testar a extração de publicações?
- A infraestrutura (Postgres no Linode, Blob Storage no Azure) já está provisionada e com credenciais disponíveis para desenvolvimento, ou precisamos de mocks/containers locais por enquanto?
- Alguma preferência de stack de testes/ferramentas .NET (ex: xUnit + Testcontainers como default), ou usar outra?
