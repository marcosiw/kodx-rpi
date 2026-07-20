# Coleção Bruno — Kodx RPI API

Desde a fase 8, os endpoints privados da API são gRPC (não REST) — ver `ai/context.md` e
`specs/Especificações - Kodx RPI.md`. No lugar de Swagger, a exploração/documentação viva é
feita pelo próprio [Bruno](https://www.usebruno.com/) usando **server reflection**
(`Grpc.AspNetCore.Server.Reflection`, habilitado em `Development`).

## Como testar os endpoints gRPC no Bruno

1. Habilite o suporte a gRPC (beta) em Preferences > Beta, na versão 2.10+ do Bruno.
2. Crie uma request do tipo gRPC apontando pro endpoint **gRPC** da API (porta 8080 por
   default, `Kestrel:Endpoints:Grpc` em `appsettings.json`) — não confundir com a porta 8081
   (`/health`, HTTP simples). As duas portas existem porque Kestrel não multiplexa HTTP/1.1 e
   HTTP/2 sem TLS de forma confiável na mesma porta (ver decisão na fase 8 em `ai/context.md`).
3. Use "Server Reflection" (em vez de importar o `.proto`) para descobrir o serviço
   `rpi.RpiService` e seus métodos — não precisa apontar pro arquivo `.proto` manualmente,
   a não ser que quiera intellisense adicional (o arquivo fonte está em
   `src/Kodx.Rpi.Api/Protos/rpi.proto`).
4. Adicione o header de autenticação como metadata da chamada: `x-api-key: <valor de
   ApiKey__Value>` (mesmo mecanismo de sempre, só que como metadata gRPC em vez de header
   HTTP puro).

## Métodos disponíveis (`rpi.RpiService`)

- `TriggerDownload` — unário. Migração do antigo `POST /rpis/{tipo}/download/{edicao?}`.
- `GetRpiHistory` — unário. Dados da edição + histórico de tentativas de processamento.
- `DownloadPdf` — server-streaming. PDF já processado, em chunks, do Blob Storage.
- `SearchRpiPublications` — server-streaming. Busca por um conjunto de números de processo
  (`numeros`, em lote — não um número por chamada), considerando número primário **e**
  secundários de cada publicação (tabela `publication_numeros`). `tipo`/`edicao` são
  opcionais: se informados, restringe a busca a uma edição específica; se omitidos, busca os
  números em todo o histórico já processado (cada `PublicationReply` traz `tipo`/`edicao` de
  onde veio, já que o resultado pode abranger várias edições).

`/health` continua um endpoint HTTP simples (sem gRPC, sem API key) — pode testar direto
como uma request REST comum no Bruno (ver `Untitled.yml` nesta coleção).
