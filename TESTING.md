# Pruebas de recuperación de contraseña

## Pruebas automatizadas

Desde la carpeta `ms-forgot-information`:

```powershell
dotnet test .\tests\ms-forgot-information.Tests\ms-forgot-information.Tests.csproj
```

Desde `dvlp-front/dvlp-web`, ejecutar las pruebas de la pantalla de correo, código y cambio de
contraseña y su cliente HTTP:

```powershell
npm test -- --watch=false `
  --include=src/app/core/services/forgot-information.service.spec.ts `
  --include=src/app/features/public/auth/forgot-password/steps/email/email.spec.ts `
  --include=src/app/features/public/auth/forgot-password/steps/code/code.spec.ts `
  --include=src/app/features/public/auth/forgot-password/steps/reset/reset.spec.ts
```

## Comprobar el frontend contra los servicios reales

El frontend web está configurado con `API_FORGOT_INFORMATION_URL=http://localhost:8091`.
`ForgotInformationService` llama a `POST /api/v1/password/forgot`, luego a
`POST /api/v1/password/forgot/verify` y finalmente a `POST /api/v1/password/reset`. Las rutas,
los nombres de los campos y la respuesta `resetToken` coinciden con la API del microservicio.

1. Confirma que SQL Server y `ms-iam` estén ejecutándose en la red Docker compartida
   `sg-services-network`.
2. Desde `Proyecto\ms-iam`, inicia o reconstruye IAM:

   ```powershell
   docker compose up -d --build
   ```

3. Desde `GuardianEscolar\ms-forgot-information`, inicia el servicio real. Su `.env` debe
   contener la conexión a la misma base de datos usada por IAM, el mismo `INTERNAL_API_KEY`,
   el secreto `OTP_HASH_PEPPER` y las variables SMTP:

   ```powershell
   docker compose up -d --build
   ```

4. Comprueba el endpoint de salud y abre el frontend en <http://localhost:4200>:

   ```powershell
   Invoke-RestMethod http://localhost:8091/health
   ```

5. En la pantalla «Olvidé mi contraseña», envía el correo de un perfil activo real. El
   microservicio consulta a IAM y SMTP envía el código a la dirección que IAM devuelve.
6. Verifica el código y establece la nueva contraseña en los siguientes pasos del frontend.

**Esta comprobación modifica la contraseña real de la cuenta.** No la ejecutes con una cuenta
si no tienes autorización para restablecer su contraseña. Para validar solo conectividad sin
enviar correo ni cambiar datos, utiliza un correo deliberadamente inexistente; el endpoint
debe responder `202 Accepted` con el mensaje genérico.

Los archivos `.env` locales de IAM y recuperación contienen secretos y están excluidos de Git.
No añadas contraseñas reales al repositorio, documentación, logs ni solicitudes de soporte.
