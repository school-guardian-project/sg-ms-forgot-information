# sg-ms-forgot-information

Microservicio .NET 10 dedicado exclusivamente a **recuperar contraseñas por correo**. Envía
un código numérico de seis dígitos por SMTP, verifica el código y solicita a `ms-iam` que
guarde la contraseña nueva. No almacena contraseñas ni ofrece cambio autenticado de
contraseña, correo o teléfono.

## Flujo

1. `POST /api/v1/password/forgot`: busca el perfil por correo y, si existe, guarda el hash del
   código y lo envía por correo. Siempre responde `202 Accepted` con el mismo mensaje, exista
   o no el correo.
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
