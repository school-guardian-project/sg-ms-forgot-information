#!/usr/bin/env bash
# Sincroniza el .proto que sg-ms-forgot-information consume desde ms-iam.
#
# El .proto es el contrato entre servicios, pero cada servicio corre su propio
# generador: no se comparte el artefacto generado. Esta copia agrega
# `option csharp_namespace` para que las clases cliente vivan en el namespace
# de quien las consume y no en el del servidor. Servicios, mensajes, campos y
# numeros de campo son identicos, asi que ambos lados hablan exactamente el
# mismo protocolo.
#
# Uso: ./sync-protos.sh   (desde backend/sg-ms-forgot-information)
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="$HERE/src/ms-forgot-information.Api/Protos"

mkdir -p "$TARGET"

# internal_profile.proto trae options de Java; se inyecta el namespace C# tras
# la linea del package sin tocar nada mas del contrato.
sed '/^package iam\.v1;/a option csharp_namespace = "ms_forgot_information.Api.Shared.Infrastructure.Grpc";' \
  "$HERE/../ms-iam/src/main/proto/internal_profile.proto" > "$TARGET/internal_profile.proto"
echo "sincronizado: internal_profile.proto -> $TARGET/internal_profile.proto"
