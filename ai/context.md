# Kodx RPI — Contexto para IA

API .NET responsável por baixar RPIs (Revista da Propriedade Industrial) do site do INPI, validar consistência, converter para txt, salvar no Blob Storage (Azure) e persistir publicações individualizadas em Postgres, servindo o sistema Kodx e outros serviços consumidores.

- Especificação completa: `specs/Especificações - Kodx RPI.md`
- Código-fonte: `src/`

Antes de iniciar qualquer trabalho nesta base, leia este arquivo. Ao tomar decisões relevantes de arquitetura, fechar dúvidas em aberto ou avançar de fase, atualize-o.

## Status atual

Fase de scaffolding inicial do repositório (ainda sem código da aplicação).

## Plano de implementação (por fases, validadas uma a uma)

1. Scaffolding do repositório (docs de IA, git, estrutura de pastas) — **em andamento**
2. Esqueleto da aplicação .NET (camadas DDD, Swagger, auth por API key, logging estruturado, Docker, CI básico)
3. Modelagem e migrations do banco Postgres (histórico de downloads/transformações, publicações em JSONB)
4. Download de RPIs do site do INPI
5. Conversão PDF → TXT
6. Armazenamento no Azure Blob Storage (seguindo estrutura/tags já existentes)
7. Extração e persistência das publicações individuais
8. Endpoints privados para o Kodx API (consulta de RPI, download de PDF, busca de publicações por edição)
9. Scripts de cronjob (Ubuntu 24.04) + documentação
10. CI/CD completo (sec scan, testes, build, deploy em versão fechada)

## Decisões tomadas

- Repositório GitHub: `git@github.com:marcosiw/kodx-rpi.git`
- Docs para IA ficam em `ai/`, com `ai/context.md` cumprindo o papel de guia principal (equivalente a um CLAUDE.md).

## Perguntas em aberto

- Existe outro repositório/serviço Kodx já em produção com convenções de Blob Storage (estrutura de pastas/tags) ou parsing de publicações que devemos seguir/portar?
- Há amostras de PDF/TXT de RPI disponíveis para desenvolver e testar a extração de publicações?
- A infraestrutura (Postgres no Linode, Blob Storage no Azure) já está provisionada e com credenciais disponíveis para desenvolvimento, ou precisamos de mocks/containers locais por enquanto?
- Alguma preferência de stack de testes/ferramentas .NET (ex: xUnit + Testcontainers como default), ou usar outra?
