# Kodx RPI — Contexto para IA

API .NET responsável por baixar RPIs (Revista da Propriedade Industrial) do site do INPI, validar consistência, converter para txt, salvar no Blob Storage (Azure) e persistir publicações individualizadas em Postgres, servindo o sistema Kodx e outros serviços consumidores.

- Especificação completa: `specs/Especificações - Kodx RPI.md`
- Código-fonte: `src/` (solução em `Kodx.Rpi.slnx`), testes em `tests/`

Antes de iniciar qualquer trabalho nesta base, leia este arquivo. Ao tomar decisões relevantes de arquitetura, fechar dúvidas em aberto ou avançar de fase, atualize-o.

## Status atual

Fase 4 (download de RPIs do INPI) implementada e **validada de ponta a ponta contra o site real do INPI** na branch `feat/inpi-download`, aguardando revisão/PR.

## Plano de implementação (por fases, validadas uma a uma)

1. ~~Scaffolding do repositório (docs de IA, git, estrutura de pastas)~~ — **concluído**
2. ~~Esqueleto da aplicação .NET (camadas DDD, Swagger, auth por API key, logging estruturado, Docker, CI básico)~~ — **concluído** (mergeado em `main` via PR #1)
3. ~~Modelagem e migrations do banco Postgres (histórico de downloads/transformações, publicações em JSONB)~~ — **concluído** (mergeado em `main` via PR #2)
4. ~~Download de RPIs do site do INPI~~ — **concluído, em validação** (branch `feat/inpi-download`)
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
- Fluxo de branches: cada fase do plano é implementada em uma branch própria a partir de `main`; ao finalizar, o merge em `main` é feito via **squash** (histórico enxuto na main, detalhado preservado na branch de origem). O PR #1 (fase 2) saiu como merge commit normal por engano; o usuário restringiu o repositório no GitHub (Settings > Pull Requests) para permitir só squash merge dali em diante.
- Esqueleto da aplicação (.NET 10, target `net10.0`):
  - Solução `Kodx.Rpi.slnx` na raiz; projetos de camada em `src/` (`Kodx.Rpi.Domain`, `Kodx.Rpi.Application`, `Kodx.Rpi.Infrastructure`, `Kodx.Rpi.Api`), testes em `tests/` (`Kodx.Rpi.Application.Tests`, `Kodx.Rpi.Infrastructure.Tests`, xUnit). Referências seguem DDD: Domain ← Application ← Infrastructure/Api.
  - Logging estruturado com Serilog + `UseSerilogRequestLogging()` para log de entrada/saída de requests. Console usa formato diferente por ambiente: em `Development`, template padrão do Serilog (texto já renderizado, legível no terminal); fora de `Development`, `CompactJsonFormatter` (JSON/CLEF, pensado para agregação de logs). No CLEF, `@mt` guarda o template bruto (não substituído) por design — os valores reais vêm como campos próprios no JSON (`RequestMethod`, `StatusCode` etc.) e `@r` traz o valor já formatado só para tokens com format specifier (ex: `{Elapsed:0.0000}`); não é um bug, é assim que ferramentas de agregação (Seq etc.) esperam consumir.
  - Auth por API key: middleware próprio (`Kodx.Rpi.Api/Security/ApiKeyMiddleware.cs`), header configurável (default `X-Api-Key`), valor lido de config/env (`ApiKey__Value`, nunca commitado). Endpoints `/health`, `/swagger` e `/favicon.ico` isentos (o favicon é isento porque o navegador o busca automaticamente ao abrir qualquer página, o que gerava um 401 de ruído no log mesmo com a página principal respondendo certo).
  - Timeout default de 30s via `AddRequestTimeouts`/`UseRequestTimeouts` (built-in do ASP.NET Core), pode ser sobrescrito por endpoint com o atributo `[RequestTimeout]` quando necessário.
  - Swagger via Swashbuckle (`AddSwaggerGen`/`UseSwagger`/`UseSwaggerUI`), habilitado em Development. Optou-se por Swashbuckle em vez do `Microsoft.AspNetCore.OpenApi` nativo do template porque este último trouxe uma dependência transitiva (`Microsoft.OpenApi` 2.0.0) com vulnerabilidade conhecida (NU1903) na versão do pacote puxada pelo template.
  - Docker multi-stage (`Dockerfile` na raiz, `.dockerignore`), imagem final expõe porta 8080.
  - CI (`.github/workflows/ci.yml`): jobs `security` (dotnet list package --vulnerable) → `test` (dotnet test) → `build` → `deploy` (só dispara em tags `v*.*.*`). O job `deploy` builda e publica a imagem no **GitHub Container Registry** (`ghcr.io/marcosiw/kodx-rpi`, tags `<versão>` e `latest`), usando o `GITHUB_TOKEN` (permissão `packages: write`) — sem action de terceiro, só `docker login`/`build`/`push` via CLI. **O deploy real na instância Linode (pull da imagem + restart do serviço) ainda não está implementado**, depende de decidir acesso/credenciais em fase futura.
  - Gatilho do CI é só `push` (branches + tags `v*.*.*`); não usamos `pull_request` porque duplicava a execução do pipeline inteiro a cada push numa branch com PR aberto para `main` (checks continuam aparecendo no PR pelo mesmo commit mesmo assim).
  - direnv: `.envrc.example` versionado como template; `.envrc` real ignorado pelo git.
  - Config local na IDE (Visual Studio/Rider): **um único mecanismo de config (variáveis de ambiente)** em todos os contextos — shell (`.envrc`/direnv), Docker/produção (env do orquestrador) e IDE. Para a IDE, que não herda o shell do direnv ao ser aberta direto (ex: ícone do Windows), usamos `src/Kodx.Rpi.Api/Properties/launchSettings.json`, que o Visual Studio/Rider já leem nativamente para variáveis de ambiente ao rodar o profile — **esse arquivo saiu do git** (contém valores locais como `ApiKey__Value`) e um `launchSettings.json.example` (com valores vazios) é o template versionado, no mesmo padrão do `.envrc.example`.
    - Avaliamos e descartamos `dotnet user-secrets`: ele só carrega em `Development` a partir de um arquivo fora do repo (`~/.microsoft/usersecrets/<GUID>/secrets.json`), nunca é publicado/empacotado e **não existe dentro de containers Docker** — seria uma segunda fonte de verdade desconectada de Docker/produção, o que o usuário preferiu evitar.

- Schema Postgres (fase 3, EF Core 10 + Npgsql, `EFCore.NamingConventions` para snake_case):
  - **Referência**: existe um repositório legado (`/mnt/e/projects/kodx-legacy`, `Kodx.Producao`, .NET/EF Core 6) que já roda em produção. Ele **não persiste** a RPI em si (entidade `Rpi` é só em memória) nem tem histórico de tentativas de download/conversão — esse é o gap que a fase 3 preenche. A tabela `publicacoes_pi` do legado guarda publicações em colunas de texto flat (`cabecalho1-3`, `conteudo`, `numero`, `pagina`, `index_inicio/fim`, `rodape`, `orgao`), ligadas à RPI só por `caderno_id` (tipo) + `edicao`, sem FK real.
  - **`RpiTipo`** (`Kodx.Rpi.Domain/Rpis/RpiTipo.cs`): reaproveita exatamente os valores do enum do legado (`Comunicados=1 ... TopografiaCircuitos=8`).
  - **`rpi_editions`**: nova tabela (o legado nunca persistiu isso) — `id`, `edicao`, `tipo`, `data_publicacao`, `created_at`; índice único em `(edicao, tipo)`.
  - **`rpi_processing_attempts`**: nova tabela cobrindo o gap de auditoria — `id`, `rpi_edition_id` (FK), `stage` (enum `ProcessingStage`: `Download`, `ConvertToTxt`, `ExtractPublications`, `UploadBlob` — 4 etapas independentes, cada uma com sua própria tentativa/status), `status` (`Success`/`Failure`), `error_message`, `started_at`, `finished_at`.
  - **`publications`**: `id`, `rpi_edition_id` (FK), `numero` (coluna real, indexada — decisão explícita do usuário, mesmo a spec sugerindo que filtragem fina ficaria por conta de outros serviços), `payload` (jsonb, via `OwnsOne(...).ToJson()` do EF Core/Npgsql — contém `Cabecalho1-3`, `Conteudo`, `TodosNumeros`, `IndexInicio/Fim`, `Rodape`, `Pagina`, `Orgao`, equivalente ao conteúdo das colunas flat do legado, sem duplicar `Numero`), `created_at`.
  - **Postgres local/testes**: só `docker-compose.yml` (Postgres 16, container `kodx-rpi-db`), **sem Testcontainers** — decisão explícita do usuário. Testes de integração em `Kodx.Rpi.Infrastructure.Tests/Persistence/KodxRpiDbContextTests.cs` assumem esse Postgres rodando (`docker compose up -d`) e aplicam `Database.MigrateAsync()` no `InitializeAsync`.
  - CI: o job `test` ganhou um Postgres 16 como *service container* do GitHub Actions (não é Testcontainers, é o mecanismo nativo de `services:` do Actions) para os testes de integração rodarem no pipeline.
  - Connection string via `ConnectionStrings__Postgres` (env var), seguindo o mesmo mecanismo único já estabelecido (`.envrc`, `launchSettings.json`/`.example`, CI). Valor local de dev: `Host=localhost;Port=5432;Database=kodx_rpi;Username=postgres;Password=postgres` (bate com o `docker-compose.yml`).
  - Migration inicial (`InitialCreate`) gerada, aplicada com `dotnet ef database update` contra o Postgres real do `docker-compose.yml` e confirmada com `\dt` (3 tabelas criadas). Os 2 testes de integração (round-trip do jsonb, tentativa com falha) passam contra esse banco real.
  - Docker precisou de ajuste de permissão no WSL: usuário não estava no grupo `docker` (`sudo usermod -aG docker marcos` + reiniciar a sessão WSL resolveu — `wsl --shutdown` no Windows ou reiniciar a máquina).

- Download de RPIs do INPI (fase 4):
  - **Não há lógica no legado** para determinar "qual é a edição atual" — construído do zero. Regra: publicação semanal (terças-feiras), calculada a partir de um par âncora configurável (`RpiSchedule:AnchorEdition`/`AnchorPublicationDate` em appsettings, hoje `2891`/`2026-06-02`) em `RpiEditionCalculator` (Domain, puro, usa `TimeProvider` — testável sem mockar relógio de verdade).
  - **Risco conhecido e documentado no código**: se o INPI pular uma semana (feriado), o cálculo desalinha até o anchor ser corrigido manualmente na config. Passar `edicao` explícita no endpoint sempre contorna isso.
  - **Endpoint**: `POST /rpis/{tipo}/download/{edicao?}` — se `edicao` vier na rota usa ela; senão calcula via `RpiEditionCalculator`. Enfileira o trabalho e responde `202 Accepted` na hora (execução em **background**, decisão explícita do usuário desde já nesta fase, não só quando encadearmos as próximas).
  - **Fila de background**: `IBackgroundTaskQueue`/`BackgroundTaskQueue` (`System.Threading.Channels`, padrão oficial "Queued background tasks" do ASP.NET Core) + `QueuedHostedService` (cria um `IServiceScope` novo por job, já que o escopo da request HTTP que enfileirou já terminou quando o job roda). Fila em memória — perde os itens pendentes se o processo reiniciar (aceitável por ora; o histórico de tentativas já concluídas fica no Postgres de qualquer forma).
  - **`RpiFileNaming`** (Domain): mapeia `RpiTipo` → prefixo de arquivo, reaproveitando exatamente os nomes do legado (`Patentes`, `Marcas`, `Desenhos_Industriais`, `Contratos_de_Tecnologia`, `Indicacoes_Geograficas`, `Comunicados`, `Programas_de_Computador`, e o typo `Topografia_de_circuto_Integrado` — precisa bater com a URL real).
  - **`InpiRpiDownloader`** (Infrastructure): `HttpClient` nomeado apontando pra `http://revistas.inpi.gov.br/pdf/{arquivo}`, timeout de 300s (`Inpi:HttpTimeoutSeconds`, alto porque roda em background). **Descoberta importante**: o INPI retorna 403 para requests sem `User-Agent` de navegador (WAF bloqueia o UA padrão de HttpClient/curl) — configurado um UA fixo (`Inpi:UserAgent`). O site também redireciona http→https (302), seguido automaticamente pelo `HttpClient` (`AllowAutoRedirect` é `true` por padrão).
  - **Validação do PDF**: checagem leve (não-vazio + assinatura `%PDF` nos primeiros bytes) — suficiente pra esta fase; extração/parsing real de conteúdo fica pras fases 5/7.
  - **Armazenamento local temporário**: `LocalDiskRpiFileStorage` grava em `RpiStorage:LocalWorkingDirectory` (default `./data/rpi/{edicao}/{arquivo}`, gitignored) — o upload pro Azure Blob Storage é a fase 6; as fases seguintes vão ler desse mesmo diretório.
  - **Testado contra o INPI real** (não só mock): edição âncora 2891/Patentes baixada com sucesso (PDF de 3.162.451 bytes, `Last-Modified` bate com a data âncora) e edição calculada automaticamente 2897/Marcas (16/07/2026, 6 semanas após o anchor — cálculo bateu certo), ambas gravando `rpi_processing_attempts` com status `Success` e o arquivo salvo localmente.

## Perguntas em aberto

- Há amostras de PDF/TXT de RPI disponíveis para desenvolver e testar a extração de publicações? (o legado dá a estrutura de dados, mas não substitui ter arquivos reais para testar o parsing)
- A infraestrutura real (Postgres no Linode, Blob Storage no Azure) já está provisionada e com credenciais disponíveis para desenvolvimento/deploy, ou seguimos só com o Postgres local via docker-compose por enquanto?
