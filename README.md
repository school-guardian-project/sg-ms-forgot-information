# sg-ms-forgot-information

Microservicio .NET 10 para **recuperar contraseñas y cambiar correos o teléfonos**.
Envía códigos por SMTP o verifica teléfonos mediante Twilio Verify y solicita a
`ms-iam` que actualice los datos. No almacena contraseñas.

## Flujo

1. `POST /api/v1/password/forgot`: busca el perfil por correo y, si existe, guarda el hash del
   código y lo envía por correo. Responde `202 Accepted` si se envía el código;
   si la cuenta no existe responde 404 con `code: account_not_found`.
2. `POST /api/v1/password/forgot/verify`: valida el código y devuelve un token opaco de
   restablecimiento.
3. `POST /api/v1/password/reset`: valida el token, actualiza la contraseña en `ms-iam` y
   consume el código solo cuando la actualización termina correctamente.

Los códigos se guardan con HMAC-SHA256, sal aleatoria y `Otp__HashPepper`; expiran, tienen
límite de intentos y no se pueden volver a verificar una vez aceptados. Solicitar otro código
deja vigente únicamente el más reciente. La API también limita solicitudes por IP y por perfil.
Ni el código ni la contraseña se escriben en los logs.

## Endpoints y cuerpos

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/v1/password/forgot` | Solicitar el código por correo |
| `POST` | `/api/v1/password/forgot/verify` | Verificar correo y código; devuelve `resetToken` |
| `POST` | `/api/v1/password/reset` | Establecer la contraseña con el token |
| `GET` | `/health` | Estado del servicio |

```json
{ "email": "persona@ejemplo.com" }
```

```json
{ "email": "persona@ejemplo.com", "code": "123456" }
```

```json
{
  "email": "persona@ejemplo.com",
  "resetToken": "token-opaco",
  "newPassword": "NuevaClave1!",
  "confirmPassword": "NuevaClave1!"
}
```

## Configurar SMTP

1. Para Gmail, activa la verificación en dos pasos y crea una **contraseña de aplicación**.
   No uses la contraseña normal de la cuenta.
2. Copia `.env.example` como `.env`. Configura SMTP, la conexión a la base de datos compartida
   con IAM, `OTP_HASH_PEPPER`, el origen CORS y la misma `INTERNAL_API_KEY` que usa
   [`ms-iam`](../../ms-iam).
3. `.env` está ignorado por Git. No compartas ni confirmes credenciales reales.

La aplicación lee la configuración de SMTP de variables jerárquicas de .NET. Docker Compose
traduce las variables `SMTP_*` del `.env` a la configuración requerida. `SMTP_ENABLE_SSL=true`
con puerto 587 negocia STARTTLS. Ajusta el puerto y TLS si tu proveedor SMTP lo requiere.

Docker Compose carga `.env` automáticamente y conecta con `http://ms-iam:8080` en la red
compartida `sg-services-network`. Perfil y contraseña pertenecen al IAM real; este servicio
solo persiste desafíos OTP. `dotnet run` **no** carga archivos `.env` por sí solo: configura
las variables en el entorno de ejecución o usa un gestor de secretos.

## Ejecutar

Compose publica un puerto fijo `8091:8080`. La web debe usar
`API_FORGOT_INFORMATION_URL=http://localhost:8091`; publicar solo `"8080"` asigna
un puerto aleatorio que no coincide con esa URL. IAM debe ofrecer las rutas internas
`/api/profiles/**` y recibir `APP_INTERNAL_API_KEY` con el mismo valor privado de
`INTERNAL_API_KEY` que envia este servicio. No basta con que funcione el login.

Con el IAM real, SQL Server y la red compartida `sg-services-network` en ejecución:

```powershell
docker compose up -d --build
Invoke-RestMethod http://localhost:8091/health
```

Para ejecutar desde el código, usa .NET 10 SDK y configura las variables de entorno requeridas:

```powershell
Set-Location .\src\ms-forgot-information.Api
dotnet run
```

## Pruebas

```powershell
dotnet test .\tests\ms-forgot-information.Tests\ms-forgot-information.Tests.csproj
```

Para volver a ejecutar las pruebas automatizadas y comprobar de forma segura la integración
contra los servicios reales, consulta [TESTING.md](./TESTING.md).


## Cambio de teléfono con Twilio Verify

Tanto el teléfono actual como el nuevo se verifican con un SMS real enviado por **Twilio Verify**. Twilio genera, envía y valida el código: este servicio nunca lo genera ni lo guarda.

### Flujo (`/api/v1/phone/change/*`)

1. `request {email, currentPhone}`: si el correo existe y `currentPhone` (E.164) coincide con el teléfono guardado (IAM `POST /api/profiles/{id}/phone/matches`), Twilio envía un SMS a ese número. Una cuenta inexistente responde 404 con `code: account_not_found`; formato inválido -> 400.
2. `verify {email, currentPhone, code}`: Twilio valida el código del teléfono actual y devuelve un `resetToken` de un solo uso.
3. `verification/request {email, resetToken, newPhone}`: valida que `newPhone` sea E.164 (`+573001234567`), y pide a Twilio enviar el SMS **exactamente a ese número**.
4. `verification/check {email, newPhone, code}`: consulta a Twilio. Solo si responde `approved` se actualiza el teléfono en IAM (`PUT /api/profiles/{id}/phone`). Cualquier otro resultado se rechaza (400) y no cambia nada.

Errores: 400 número/código inválido o vencido, 429 demasiados intentos/solicitudes, 503 Twilio no configurado o no disponible.

### Configurar Twilio

1. En la consola de Twilio crea un **Verify Service** (Verify > Services) y copia su SID (`VA...`).
2. Crea una **API Key** estándar (Account > API keys & tokens) y copia el SID (`SK...`) y el secret (solo se muestra una vez).
3. Define en `.env` (nunca en el repositorio):

```
TWILIO_ACCOUNT_SID=AC...
TWILIO_API_KEY=SK...
TWILIO_API_SECRET=...
TWILIO_VERIFY_SERVICE_SID=VA...
```

4. Cuenta *trial*: Twilio solo envía SMS a números verificados (Console > Phone Numbers > Verified Caller IDs) y antepone un texto de prueba. Habilita además el país destino en Messaging > Geo permissions.
5. Reconstruye: `docker compose up -d --build`.

Sin estas variables el servicio arranca igual; solo `verification/request` y `verification/check` responden 503.

### Probar

- Automático: `dotnet test` (usa un `ISmsVerificationService` falso; no llama a Twilio).
- Real: desde el front, Perfil > cambiar teléfono, con un número autorizado en Twilio. En los logs aparece el teléfono enmascarado; nunca el código ni los secretos.
- La columna `UserManagement.Person.Phone` es `BIGINT` (antes `INT`) para guardar números de 10+ dígitos.