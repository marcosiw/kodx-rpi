#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEFAULT_OUT_DIR="$SCRIPT_DIR/../certs"

CA_DAYS=3650
CERT_DAYS=825
SERVER_SAN="${SERVER_SAN:-DNS:localhost,IP:127.0.0.1}"

usage() {
  cat <<EOF
Gera a CA privada e os certificados usados no mTLS do endpoint gRPC (porta 8080).
Ver ai/context.md pra decisão completa.

Uso:
  $0 init [OUT_DIR] [--force]
      Gera CA + certificado de servidor + um certificado de cliente de exemplo.
      Recusa rodar se já existir uma CA em OUT_DIR, a menos que --force seja
      passado (isso invalida qualquer certificado já emitido com a CA antiga).

  $0 client <nome> [OUT_DIR]
      Emite mais um certificado de cliente reaproveitando a CA já existente
      (pra onboarding de outro consumidor), sem tocar em CA/servidor.

Variáveis de ambiente:
  SERVER_SAN   Subject Alternative Names do certificado de servidor.
               Default: "$SERVER_SAN"
               Em produção, inclua o host/IP real do Linode, ex:
               SERVER_SAN="DNS:localhost,IP:127.0.0.1,DNS:rpi.kodx.example.com,IP:203.0.113.10"

OUT_DIR default: $DEFAULT_OUT_DIR (já está no .gitignore — nada aqui deve ser commitado)
EOF
}

require_openssl() {
  command -v openssl >/dev/null 2>&1 || { echo "openssl não encontrado no PATH." >&2; exit 1; }
}

gen_ca() {
  local out_dir="$1"
  openssl genrsa -out "$out_dir/ca.key" 4096
  openssl req -x509 -new -nodes -key "$out_dir/ca.key" -sha256 -days "$CA_DAYS" \
    -subj "/CN=Kodx RPI mTLS CA" \
    -addext "basicConstraints=critical,CA:TRUE" \
    -out "$out_dir/ca.crt"
}

gen_leaf() {
  local out_dir="$1" name="$2" cn="$3" eku="$4" san="${5:-}"
  openssl genrsa -out "$out_dir/$name.key" 2048
  openssl req -new -key "$out_dir/$name.key" -subj "/CN=$cn" -out "$out_dir/$name.csr"

  local extfile="$out_dir/.$name.ext.tmp"
  {
    echo "[ v3_ext ]"
    echo "basicConstraints=CA:FALSE"
    echo "keyUsage=digitalSignature,keyEncipherment"
    echo "extendedKeyUsage=$eku"
    [ -n "$san" ] && echo "subjectAltName=$san"
  } > "$extfile"

  openssl x509 -req -in "$out_dir/$name.csr" -CA "$out_dir/ca.crt" -CAkey "$out_dir/ca.key" \
    -CAcreateserial -out "$out_dir/$name.crt" -days "$CERT_DAYS" -sha256 \
    -extfile "$extfile" -extensions v3_ext

  rm -f "$out_dir/$name.csr" "$extfile"
}

cmd_init() {
  local out_dir="$DEFAULT_OUT_DIR"
  local force=false
  for arg in "$@"; do
    case "$arg" in
      --force) force=true ;;
      *) out_dir="$arg" ;;
    esac
  done

  mkdir -p "$out_dir"

  if [ -f "$out_dir/ca.key" ] && [ "$force" != true ]; then
    echo "Já existe uma CA em $out_dir/ca.key — use --force pra recriar (isso invalida certificados já emitidos com ela)." >&2
    exit 1
  fi

  gen_ca "$out_dir"
  gen_leaf "$out_dir" "server" "kodx-rpi-server" "serverAuth" "$SERVER_SAN"
  gen_leaf "$out_dir" "client" "kodx-api-client" "clientAuth"

  openssl pkcs12 -export -out "$out_dir/client.pfx" -inkey "$out_dir/client.key" \
    -in "$out_dir/client.crt" -certfile "$out_dir/ca.crt" -passout pass:kodx-rpi

  chmod 600 "$out_dir"/*.key

  cat <<EOF

Gerado em $out_dir:
  ca.key / ca.crt          - CA privada (nunca compartilhar ca.key)
  server.key / server.crt  - certificado do servidor gRPC
                             (Grpc:Mtls:ServerCertificatePath / ServerKeyPath)
  client.key / client.crt  - certificado de cliente de exemplo
                             (Grpc:Mtls:ClientCaCertificatePath = ca.crt, do lado do servidor)
  client.pfx               - mesmo client, em PKCS#12 (senha: kodx-rpi) pra ferramentas
                             que exigem esse formato em vez de PEM separado

Nada aqui deve ser commitado - o diretório já está no .gitignore.
EOF
}

cmd_client() {
  local name="${1:?informe um nome pro cliente, ex: rpi-consumer-2}"
  local out_dir="${2:-$DEFAULT_OUT_DIR}"

  case "$name" in
    ca|server) echo "Nome reservado ('$name') - escolha outro nome pro cliente." >&2; exit 1 ;;
  esac

  if [ ! -f "$out_dir/ca.key" ]; then
    echo "Não achei $out_dir/ca.key - rode '$0 init' primeiro." >&2
    exit 1
  fi

  gen_leaf "$out_dir" "$name" "$name" "clientAuth"
  chmod 600 "$out_dir/$name.key"
  echo "Gerado $out_dir/$name.key / $out_dir/$name.crt"
}

require_openssl

case "${1:-}" in
  init) shift; cmd_init "$@" ;;
  client) shift; cmd_client "$@" ;;
  *) usage; exit 1 ;;
esac
