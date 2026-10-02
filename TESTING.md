# ms-forgot-information

Microservicio encargado de gestionar operaciones relacionadas con la recuperación y actualización de información sensible del usuario:

* Recuperación de contraseña.
* Cambio de contraseña.
* Cambio de correo electrónico.
* Cambio de número telefónico.
* Generación y validación de códigos OTP.
* Protección contra múltiples solicitudes de códigos.
* Integración con el servicio de identidad.
* Publicación de eventos de dominio.

## 1. Requisitos

Antes de ejecutar el microservicio asegúrate de tener instalado:

* .NET SDK 10.
* Git.
* Una instancia de la base de datos configurada.
* Los servicios externos requeridos por el proyecto.
* Variables/configuración necesarias para JWT, OTP y servicios externos.

Verifica la versión de .NET:

```bash
dotnet --version
```

El proyecto está configurado para:

```text
net10.0
```

---

# 2. Clonar el proyecto

```bash
git clone <URL_DEL_REPOSITORIO>
cd ms-forgot-information
```

Si el proyecto ya está clonado:

```bash
cd ms-forgot-information
```

---

# 3. Cambiar a la rama correspondiente

Por ejemplo:

```bash
git checkout feature/forgot-information
```

Verifica la rama actual:

```bash
git branch --show-current
```

---

# 4. Restaurar dependencias

Ejecuta:

```bash
dotnet restore
```

---

# 5. Compilar el proyecto

```bash
dotnet build
```

Si quieres hacer una compilación limpia:

```bash
dotnet clean
dotnet restore
dotnet build
```

---

# 6. Configuración

La configuración principal se encuentra en:

```text
src/ms-forgot-information.Api/appsettings.json
```

También pueden utilizarse:

```text
appsettings.Development.json
```

o variables de entorno.

## Configuración OTP

La sección correspondiente tiene una estructura similar a:

```json
"Otp": {
  "CodeLength": 6,
  "ExpirationMinutes": 10,
  "MaxAttempts": 5
}
```

Los valores exactos deben corresponder a la configuración utilizada por el proyecto.

### Parámetros importantes

| Parámetro           | Descripción                             |
| ------------------- | --------------------------------------- |
| `CodeLength`        | Cantidad de dígitos del código OTP      |
| `ExpirationMinutes` | Tiempo de validez del código            |
| `MaxAttempts`       | Cantidad máxima de intentos incorrectos |

---

# 7. Ejecutar el microservicio

Desde la raíz del proyecto:

```bash
dotnet run --project src/ms-forgot-information.Api
```

También puedes ejecutar específicamente con Development:

```bash
dotnet run --project src/ms-forgot-information.Api --environment Development
```

La API quedará disponible en el puerto configurado.

En el entorno utilizado durante las pruebas:

```text
http://localhost:8091
```

---

# 8. Ejecutar y observar los logs

Es importante mantener una terminal ejecutando el microservicio:

```bash
dotnet run --project src/ms-forgot-information.Api --environment Development
```

No ejecutes `Ctrl+C` mientras estés realizando una prueba que todavía necesite consultar los logs.

Para ejecutar los `curl`, abre **otra terminal Git Bash**.

### Terminal 1

```bash
dotnet run --project src/ms-forgot-information.Api --environment Development
```

### Terminal 2

Ejecuta las peticiones:

```bash
curl ...
```

De esta forma puedes observar los logs continuamente sin detener la API.

---

# 9. JWT

Para los endpoints autenticados necesitas un JWT válido.

En Git Bash:

```bash
export TOKEN="TU_TOKEN"
```

Comprueba que la variable existe:

```bash
echo "$TOKEN"
```

No debes subir tokens reales al repositorio.

Evita:

```bash
git add .
```

si tienes archivos locales que contienen secretos.

---

# 10. Solicitar código para cambio de contraseña

Endpoint:

```text
POST /api/v1/password/change/request-code
```

Comando:

```bash
curl -X POST http://localhost:8091/api/v1/password/change/request-code \
  -H "Authorization: Bearer $TOKEN"
```

Respuesta esperada:

```json
{
  "message": "Código enviado a tu correo registrado."
}
```

El código OTP debe llegar al medio configurado por el sistema.

---

# 11. Confirmar cambio de contraseña

El DTO requiere:

* `currentPassword`
* `newPassword`
* `confirmPassword`
* `code`

Ejemplo:

```bash
curl -X POST http://localhost:8091/api/v1/password/change \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "currentPassword": null,
    "newPassword": "NuevaClave2026!",
    "confirmPassword": "NuevaClave2026!",
    "code": "123456"
  }'
```

Reemplaza:

```text
123456
```

por el código recibido.

> Si estás realizando el cambio mediante contraseña actual en lugar de OTP, utiliza la estructura correspondiente al DTO y no mezcles ambos mecanismos.

---

# 12. Cambio de teléfono

## 12.1 Solicitar código

Endpoint:

```text
POST /api/v1/phone/change/request
```

Ejemplo:

```bash
curl -X POST http://localhost:8091/api/v1/phone/change/request \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "newPhone": "+573001112233"
  }'
```

Respuesta esperada:

```json
{
  "message": "Código enviado al nuevo teléfono."
}
```

---

# 13. Confirmar cambio de teléfono

Endpoint:

```text
POST /api/v1/phone/change/confirm
```

Ejemplo:

```bash
curl -X POST http://localhost:8091/api/v1/phone/change/confirm \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "123456"
  }'
```

Reemplaza el código por el OTP recibido en el teléfono.

Respuesta esperada:

```json
{
  "message": "Teléfono actualizado correctamente."
}
```

---

# 14. Cambio de correo electrónico

## Solicitar código

Endpoint:

```text
POST /api/v1/email/change/request
```

Ejemplo:

```bash
curl -X POST http://localhost:8091/api/v1/email/change/request \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "newEmail": "nuevo-correo@example.com"
  }'
```

## Confirmar código

Endpoint:

```text
POST /api/v1/email/change/confirm
```

Ejemplo:

```bash
curl -X POST http://localhost:8091/api/v1/email/change/confirm \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "123456"
  }'
```

---

# 15. Flujo general de OTP

El flujo de verificación es:

```text
Solicitud
   |
   v
Generación OTP
   |
   v
Envío del código
   |
   v
Usuario introduce código
   |
   v
POST /confirm
   |
   +---- código incorrecto ---> aumenta AttemptCount
   |
   +---- máximo de intentos -> Locked
   |
   +---- código expirado ----> Expired
   |
   +---- código correcto ----> Verified
                              |
                              v
                           Complete
                              |
                              v
                           Consumed
```

El servicio central que administra este comportamiento es:

```text
src/ms-forgot-information.Api/Shared/Application/Services/VerificationCodeService.cs
```

---

# 16. Máximo de intentos

Actualmente la configuración contempla:

```text
MaxAttempts = 5
```

Los intentos incorrectos se registran en:

```text
VerificationRequest.AttemptCount
```

Cuando se alcanza el máximo:

```text
VerificationStatus.Locked
```

y se devuelve un error similar a:

```json
{
  "message": "Se superó el número máximo de intentos. Solicita un nuevo código."
}
```

---

# 17. Rate limiting

Existen dos niveles de protección.

## Rate limit HTTP

En:

```text
src/ms-forgot-information.Api/Program.cs
```

se encuentra la política:

```text
otp-public
```

Actualmente está configurada con:

```text
PermitLimit = 10
QueueLimit = 0
```

Esto proporciona una protección adicional por IP.

## Rate limit por perfil

El control principal de solicitudes OTP se encuentra en:

```text
src/ms-forgot-information.Api/Shared/Application/Services/VerificationCodeService.cs
```

El repositorio utilizado es:

```text
src/ms-forgot-information.Api/Shared/Infrastructure/Persistence/Repository/VerificationRequestRepository.cs
```

Si se excede el límite se devuelve:

```text
429 Too Many Requests
```

con un mensaje similar a:

```text
Se han solicitado demasiados códigos. Intenta de nuevo más tarde.
```

---

# 18. ¿Qué hacer si aparece "demasiados códigos"?

Si durante una prueba aparece:

```text
Se han solicitado demasiados códigos. Intenta de nuevo más tarde.
```

no sigas ejecutando el mismo `curl` repetidamente.

Primero identifica el límite configurado en:

```text
VerificationCodeService.cs
```

y revisa los registros de:

```text
VerificationRequest
```

Si estás desarrollando localmente y necesitas limpiar únicamente los datos de prueba, hazlo directamente sobre la base de datos de desarrollo siguiendo las migraciones/esquema del proyecto.

No elimines registros de producción para solucionar un problema de pruebas.

---

# 19. Pruebas de código incorrecto

Para comprobar que un código incorrecto es rechazado:

```bash
curl -X POST http://localhost:8091/api/v1/phone/change/confirm \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "000000"
  }'
```

Debe producir una respuesta de código inválido.

No repitas esta prueba más veces que el límite configurado, porque eventualmente la solicitud OTP puede quedar bloqueada.

---

# 20. Prueba de código correcto

Primero solicita un código nuevo:

```bash
curl -X POST http://localhost:8091/api/v1/phone/change/request \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "newPhone": "+573001112233"
  }'
```

Después obtén el código enviado.

Finalmente:

```bash
curl -X POST http://localhost:8091/api/v1/phone/change/confirm \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "CODIGO_REAL"
  }'
```

---

# 21. Importante: no detener la API durante la prueba

Si necesitas revisar los logs mientras ejecutas los `curl`, utiliza dos terminales.

### Terminal 1 — API

```bash
dotnet run --project src/ms-forgot-information.Api --environment Development
```

Déjala ejecutándose.

### Terminal 2 — pruebas

```bash
export TOKEN="TU_TOKEN"

curl ...
```

No hagas:

```text
Ctrl+C
```

en la terminal de la API hasta terminar las pruebas.

`Ctrl+C` detiene el proceso de `dotnet run`. Si además utilizas un flujo de desarrollo donde el token o los recursos temporales dependen del proceso, esto puede hacer que tu prueba deje de ser válida.

---

# 22. Revisar logs

Para identificar solicitudes OTP:

```bash
grep -R -n -i "Verification\|OTP\|rate\|limit\|TooMany" \
  --include="*.cs" \
  src
```

Para revisar específicamente la implementación:

```bash
sed -n '1,180p' src/ms-forgot-information.Api/Shared/Application/Services/VerificationCodeService.cs
```

Para revisar el controlador de teléfono:

```bash
sed -n '1,140p' src/ms-forgot-information.Api/Phone/Infrastructure/Controller/PhoneController.cs
```

Para revisar la confirmación:

```bash
sed -n '1,140p' src/ms-forgot-information.Api/Phone/Application/UseCase/ConfirmPhoneChangeService.cs
```

---

# 23. Ejecutar pruebas automatizadas

Si existen proyectos de pruebas:

```bash
dotnet test
```

Para ejecutar con mayor detalle:

```bash
dotnet test --verbosity normal
```

Para ejecutar una solución específica:

```bash
dotnet test <ruta-del-proyecto-de-tests>
```

---

# 24. Verificar el estado de Git

Antes de hacer commit:

```bash
git status
```

Revisa especialmente que no aparezcan:

```text
.env
tokens
secretos
passwords
connection strings
logs
bin/
obj/
```

---

# 25. Limpiar compilación

Si existen problemas relacionados con archivos generados:

```bash
dotnet clean
```

Después:

```bash
dotnet restore
dotnet build
```

También puedes eliminar los directorios generados si es necesario:

```bash
rm -rf src/*/bin
rm -rf src/*/obj
```

Después vuelve a ejecutar:

```bash
dotnet restore
dotnet build
```

---

# 26. Flujo recomendado para una prueba completa

## Terminal 1

```bash
cd ~/Desktop/Proyecto/GuardianEscolar/ms-forgot-information
```

```bash
dotnet run --project src/ms-forgot-information.Api --environment Development
```

## Terminal 2

```bash
cd ~/Desktop/Proyecto/GuardianEscolar/ms-forgot-information
```

Configurar JWT:

```bash
export TOKEN="TU_TOKEN"
```

Solicitar OTP:

```bash
curl -X POST http://localhost:8091/api/v1/phone/change/request \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "newPhone": "+573001112233"
  }'
```

Esperar el código.

Probar código incorrecto:

```bash
curl -X POST http://localhost:8091/api/v1/phone/change/confirm \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "000000"
  }'
```

Luego solicitar un código nuevo si el diseño de la prueba requiere una nueva solicitud:

```bash
curl -X POST http://localhost:8091/api/v1/phone/change/request \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "newPhone": "+573001112233"
  }'
```

Finalmente confirmar utilizando el código real:

```bash
curl -X POST http://localhost:8091/api/v1/phone/change/confirm \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "CODIGO_REAL"
  }'
```

Respuesta esperada:

```json
{
  "message": "Teléfono actualizado correctamente."
}
```

---

# 27. Estructura relevante

```text
src/
└── ms-forgot-information.Api/
    ├── Password/
    │   ├── Application/
    │   ├── Domain/
    │   └── Infrastructure/
    │
    ├── Phone/
    │   ├── Application/
    │   ├── Domain/
    │   └── Infrastructure/
    │
    ├── Email/
    │   ├── Application/
    │   ├── Domain/
    │   └── Infrastructure/
    │
    ├── Shared/
    │   ├── Application/
    │   │   ├── Otp/
    │   │   ├── Options/
    │   │   ├── Services/
    │   │   └── Validation/
    │   │
    │   ├── Domain/
    │   │   ├── Exceptions/
    │   │   ├── Model/
    │   │   └── Port/
    │   │
    │   └── Infrastructure/
    │       ├── Middleware/
    │       ├── Persistence/
    │       └── Security/
    │
    ├── Program.cs
    └── appsettings.json
```

---

# 28. Endpoints principales

| Método | Endpoint                               | Autenticación | Función                                 |
| ------ | -------------------------------------- | ------------- | --------------------------------------- |
| `POST` | `/api/v1/password/change/request-code` | JWT           | Solicitar OTP para cambio de contraseña |
| `POST` | `/api/v1/password/change`              | JWT           | Cambiar contraseña                      |
| `POST` | `/api/v1/phone/change/request`         | JWT           | Solicitar OTP para cambio de teléfono   |
| `POST` | `/api/v1/phone/change/confirm`         | JWT           | Confirmar cambio de teléfono            |
| `POST` | `/api/v1/email/change/request`         | JWT           | Solicitar OTP para cambio de correo     |
| `POST` | `/api/v1/email/change/confirm`         | JWT           | Confirmar cambio de correo              |

---

# 29. Errores frecuentes

### 400 — Validation Error

Ejemplo:

```json
{
  "errors": {
    "ConfirmPassword": [
      "The ConfirmPassword field is required."
    ]
  }
}
```

Solución: revisar que el JSON enviado contenga todos los campos obligatorios del DTO.

---

### 401 — Unauthorized

Revisar:

```bash
echo "$TOKEN"
```

Y comprobar que la petición incluya:

```text
Authorization: Bearer $TOKEN
```

---

### 429 — Too Many Requests

Puede deberse a:

* demasiadas solicitudes OTP;
* demasiados intentos de validación;
* rate limiting por IP;
* rate limiting por perfil;
* una solicitud OTP anterior todavía activa.

No continuar enviando solicitudes repetidamente. Revisar primero el estado de `VerificationRequest`.

---

### Código inválido

Si aparece:

```text
El código de verificación es inválido.
```

comprueba que:

1. Estás utilizando el último código generado.
2. No haya expirado.
3. No hayas solicitado otro código posteriormente.
4. No hayas alcanzado `MaxAttempts`.

---

### Código expirado

Si aparece:

```text
El código de verificación ha expirado.
```

solicita un nuevo código y utiliza únicamente el último generado.

---

# 30. Seguridad

Nunca almacenar directamente en el repositorio:

```text
JWT
passwords
API keys
SMTP credentials
connection strings
OTP secrets
HashPepper
```

Utilizar variables de entorno o mecanismos seguros de configuración.

Para pruebas locales:

```bash
export TOKEN="..."
```

Al terminar:

```bash
unset TOKEN
```

---

# 31. Comandos rápidos

### Restaurar

```bash
dotnet restore
```

### Compilar

```bash
dotnet build
```

### Ejecutar

```bash
dotnet run --project src/ms-forgot-information.Api --environment Development
```

### Ejecutar pruebas

```bash
dotnet test
```

### Ver rama

```bash
git branch --show-current
```

### Ver cambios

```bash
git status
```

### Buscar rate limiting

```bash
grep -R -n -i "rate\|limit\|TooMany" --include="*.cs" src
```

### Limpiar

```bash
dotnet clean
```

---

# 32. Flujo resumido

```text
1. dotnet restore
        ↓
2. dotnet build
        ↓
3. Terminal 1:
   dotnet run --project src/ms-forgot-information.Api --environment Development
        ↓
4. Terminal 2:
   export TOKEN="..."
        ↓
5. POST /change/request
        ↓
6. recibir OTP
        ↓
7. POST /change/confirm
        ↓
8. verificar respuesta
        ↓
9. revisar logs
        ↓
10. git status
```

Con este flujo se evita detener accidentalmente la API mientras se realizan las pruebas de OTP y se pueden ejecutar los `curl` desde una segunda terminal.
