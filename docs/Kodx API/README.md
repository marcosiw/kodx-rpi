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

## Configurando mTLS pra testar

Desde a fase 13, o endpoint gRPC (porta 8080) sempre exige TLS e, por padrão
(`Grpc:Mtls:Enabled=true`), também um certificado de cliente — ver decisão completa em
`ai/context.md`. Em `Development` essa exigência de certificado de cliente vem desligada por
padrão (`appsettings.Development.json`), então rodando localmente o passo abaixo normalmente
não é necessário; ele importa pra testar contra um ambiente com `Grpc:Mtls:Enabled=true`.

1. Gere os certificados (se ainda não tiver): `./scripts/generate-mtls-certs.sh init` — cria
   `./certs/ca.crt`, `./certs/client.crt`/`client.key` e `./certs/client.pfx`.
2. **Não verificamos ainda se o cliente gRPC (beta) do Bruno aceita configurar um certificado
   de cliente pra mTLS** — checar nas configurações da request/coleção antes de depender
   disso; se não for suportado, use `grpcurl` como alternativa garantida:
   ```
   grpcurl -cacert certs/ca.crt -cert certs/client.crt -key certs/client.key \
     -H 'x-api-key: <valor de ApiKey__Value>' localhost:8080 list
   ```
   Se o Bruno exigir PKCS#12 em vez de PEM separado, use `certs/client.pfx` (senha `kodx-rpi`).

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
