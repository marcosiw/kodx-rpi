# scripts/

## generate-mtls-certs.sh

Gera a CA privada e os certificados usados no mTLS do endpoint gRPC (porta 8080,
`Grpc:Mtls` em `appsettings.json`). Decisão completa em `ai/context.md`.

Pré-requisito: `openssl` no PATH.

```bash
# Primeira vez (gera ./certs/ na raiz do repo, já no .gitignore):
./scripts/generate-mtls-certs.sh init

# Emitir certificado pra outro cliente, reaproveitando a CA já existente:
./scripts/generate-mtls-certs.sh client rpi-consumer-2
```

Arquivos gerados em `./certs/` e onde cada um é usado:

| Arquivo                  | Uso                                                                  |
|---------------------------|-----------------------------------------------------------------------|
| `ca.key`                 | Chave privada da CA - nunca compartilhar, nunca commitar             |
| `ca.crt`                 | `Grpc__Mtls__ClientCaCertificatePath` (servidor valida clientes contra ela) |
| `server.key`/`server.crt`| `Grpc__Mtls__ServerKeyPath` / `Grpc__Mtls__ServerCertificatePath`     |
| `client.key`/`client.crt`| Certificado de cliente de exemplo, pra testar com `grpcurl`/ferramentas que aceitam PEM |
| `client.pfx`             | Mesmo client em PKCS#12 (senha `kodx-rpi`), pra ferramentas que exigem esse formato |

Ver `.envrc.example` pras variáveis de ambiente que apontam pra esses arquivos, e
`docs/Kodx API/README.md` pra como usar o certificado de cliente com o Bruno/grpcurl.
