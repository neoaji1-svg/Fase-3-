# Sistema de Gestión de Reservas de Espacios Universitarios (UTP - FISC)
**Proyecto Semestral — Fase 3: Implementación (prototipo funcional)**
**Materia:** Ingeniería de Software · **Profesora:** Ing. Elba Valderrama
**Integrantes:** Justin Okada (8-1028-886), Gabriel Jimenez (8-1027-2456), Maria Townshend (8-1027-1450), Neo Aji (8-969-172)

---

## 1. Arquitectura y stack tecnológico
| Aspecto | Decisión |
|---|---|
| Arquitectura | Cliente-Servidor (cliente de escritorio ↔ servidor SQL Server) + **MVC** (coherente con ADR-01, Fase 2) |
| Lenguaje | **C# 7** sobre **.NET Framework 4.8** |
| Interfaz | **Windows Forms** (`Views/`) |
| Persistencia | **SQL Server** (LocalDB, Express o instancia completa) con ADO.NET (`System.Data.SqlClient`) |
| Concurrencia (CRT-02) | Transacción `Serializable` + `WITH (UPDLOCK, HOLDLOCK)` al validar solapamiento y límite semanal |
| Seguridad (RNF-03) | Contraseñas con **PBKDF2-HMAC-SHA256** (100 000 iteraciones, salt aleatorio de 16 bytes); consultas 100 % parametrizadas (CRT-03) |
| Dependencias externas | Ninguna (sin paquetes NuGet) |

### Estructura del repositorio
```
ReservasUTP.sln
├── ReservasUTP/                     ← Aplicación Windows Forms
│   ├── App.config                   ← Cadena de conexión SQL Server
│   ├── Program.cs                   ← Punto de entrada (inicializa BD, ciclo login → catálogo)
│   ├── Config/AppConfig.cs          ← Bloques horarios y reglas RF-05 / RF-07
│   ├── Database/schema_sqlserver.sql← DDL T-SQL (idempotente)
│   ├── Models/                      ← Modelo (Usuario, Espacio, Reserva, ServicioCorreo, Db, DatosDemostracion)
│   ├── Controllers/                 ← Controladores (CtrlAuth, CtrlConsultarEspacio, CtrlConsultarHorario, CtrlCrearReserva)
│   └── Views/                       ← Formularios (FrmLogin, FrmCatalogo, FrmDisponibilidad, FrmConfirmacion, FrmMisReservas)
└── ReservasUTP.Pruebas/             ← Consola con la suite de pruebas de los CU (BD aislada)
```

---

## 2. Guía de ejecución
### Requisitos
- Windows 10/11 con **Visual Studio 2019/2022** (carga de trabajo *Desarrollo de escritorio de .NET*).
- **SQL Server**: LocalDB (se instala con Visual Studio) **o** SQL Server Express / Developer.

### Pasos
1. Abrir `ReservasUTP.sln` en Visual Studio.
2. (Opcional) Ajustar la cadena de conexión en `ReservasUTP/App.config`:
   - LocalDB (por defecto): `Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ReservasUTP;Integrated Security=True`
   - SQL Express: `Data Source=.\SQLEXPRESS;Initial Catalog=ReservasUTP;Integrated Security=True`
   - Usuario SQL: `Data Source=localhost;Initial Catalog=ReservasUTP;User ID=sa;Password=...`
3. Establecer **ReservasUTP** como proyecto de inicio y pulsar **F5**.
4. En el primer arranque la aplicación **crea la base de datos**, ejecuta `schema_sqlserver.sql` y carga los **datos de demostración** automáticamente.
5. Para reiniciar los datos: ejecutar `ReservasUTP.exe --reset` (o en VS: *Propiedades → Depurar → Argumentos de la línea de comandos* = `--reset`).

### Pruebas automatizadas
Establecer **ReservasUTP.Pruebas** como proyecto de inicio y ejecutar con **Ctrl+F5**. Usa la BD aislada `ReservasUTP_Pruebas` (se borra y recrea en cada corrida) y muestra `[PASS]/[FAIL]` por prueba.

---

## 3. Credenciales de demostración
| Cédula | Clave | Nombre | Rol | Reglas (RF-05 / RF-07) |
|---|---|---|---|---|
| `1-1-1` | `utp2026` | Carlos Mendoza | Estudiante Regular | Acceso general (6 h/sem); bloqueado en Cubículos de Tesis y Auditorios |
| `8-1028-886` | `tesista2026` | Justin Okada | Estudiante Tesista | Cubículos de Tesis (10 h/sem) + áreas generales (6 h/sem) |
| `8-1027-2456` | `clave2026` | Gabriel Jimenez | Estudiante Regular | Acceso general (6 h/sem) |
| `8-500-101` | `docente2026` | Ing. Elba Valderrama | Docente | Auditorios (8 h/sem), aulas/biblioteca (12 h/sem) |
| `8-400-202` | `admin2026` | Lic. Roberto Salas | Administrador | Acceso total (40 h/sem) |

Datos semilla: 14 espacios en 6 categorías (uno **Inhabilitado**: *Salón - 5 (Mantenimiento)*) y 7 reservas el **lunes 19-oct-2026** en los Salones 1-4 de Biblioteca (misma cuadrícula del prototipo de Fase 1). La pantalla de disponibilidad abre por defecto en esa fecha.

---

## 4. Guion de evidencia funcional (capturas sugeridas)
| # | Caso | Pasos en la aplicación | Resultado esperado |
|---|---|---|---|
| 1 | CU-01 | Login con `1-1-1` / clave errónea | Mensaje "Credenciales institucionales inválidas" |
| 2 | CU-02 | Login `1-1-1` / `utp2026` | Catálogo agrupado en 6 categorías, 13 espacios (sin el inhabilitado) |
| 3 | CU-03 | Seleccionar *Salón - 4* → Consultar disponibilidad (19/10/2026, Diaria) | Cuadrícula Salón 1-5; Salón-4 08:00 **Ocupado** (rojo), 12:00 **Disponible** (verde) |
| 4 | CU-03 FA-01 | Vista = Semanal | Columnas Lun 2026-10-19 … Dom 2026-10-25 |
| 5 | CU-03 FA-02 | Cambiar espacio en el combo | Se recarga la cuadrícula del nuevo espacio |
| 6 | CU-03 EX-01 | Elegir *Salón - 5 (Mantenimiento) [Inhabilitado]* | Alerta EX-01, botón de reserva deshabilitado |
| 7 | CU-03 EX-02 | Marcar "Simular fallo de BD" → Actualizar | Alerta EX-02, no se modifica ningún dato |
| 8 | CU-04 | Clic en Salón-4 12:00 (Disponible) → Confirmar reserva | Ventana "Reserva Realizada con Éxito" + correo registrado en `logs\notificaciones_correo.log` |
| 9 | CU-04 EX-01 | Login `8-1028-886`, intentar Salón-4 19/10 12:00-13:00 | "EX-01 (Horario ocupado)" |
| 10 | CU-04 EX-02 / FA-01 | `1-1-1` intenta *Cubículo de Tesis A-01*; luego `8-1028-886` lo reserva | Rechazo por rol / éxito para tesista |
| 11 | CU-04 EX-03 / FA-02 | `1-1-1`: 20/10 08:00-13:00 (llega a 6 h) y luego 21/10 08:00-09:00 | Primera aceptada, segunda "EX-03 (Límite semanal excedido)" |
| 12 | CU-04 EX-04 | Marcar "Simular fallo en servicio SMTP" y reservar | Reserva confirmada con aviso EX-04 (se conserva en BD) |
| 13 | RNF-01 | Ventana de confirmación | Muestra tiempo de procesamiento (< 3000 ms) |
| 14 | Suite | Ejecutar `ReservasUTP.Pruebas` | 8/8 `[PASS]` |

---

## 5. Matriz de trazabilidad actualizada (hasta archivo / clase / método)
| RF | CU | Componente (Fase 2) | Archivo · Clase | Métodos | Tablas | Vista | Prueba | Estado |
|---|---|---|---|---|---|---|---|---|
| RF-01 | CU-01 Iniciar sesión | Controlador de Auth | `Controllers/CtrlAuth.cs` · `CtrlAuth`; `Models/Usuario.cs` · `Usuario`; `Models/Db.cs` · `Seguridad` | `IniciarSesion()`, `CerrarSesion()`, `Usuario.Autenticar()`, `Seguridad.VerifyPassword()` | Usuario | `FrmLogin` | test_01 | Implementado |
| RF-02 | CU-02 Consultar espacios disponibles | CtrlConsultarEspacio | `Controllers/CtrlConsultarEspacio.cs` · `CtrlConsultarEspacio`; `Models/Espacio.cs` | `MostrarEspacios()`, `Espacio.ObtenerHabilitados()` | Espacio | `FrmCatalogo` | test_02 | Implementado |
| RF-03 | CU-03 Consultar disponibilidad | CtrlConsultarHorario | `Controllers/CtrlConsultarHorario.cs` · `CtrlConsultarHorario`; `Models/Reserva.cs` | `ConsultarDisponibilidad()`, `Espacio.ObtenerEstado()`, `Reserva.ObtenerReservas()`, `Reserva.ObtenerReservasRango()` | Espacio, Reserva | `FrmDisponibilidad` | test_03 | Implementado |
| RF-04 | CU-04 Crear reserva | CtrlCrearReserva | `Controllers/CtrlCrearReserva.cs` · `CtrlCrearReserva` | `ProcesarReserva()`, `Espacio.SigueHabilitado()`, `Reserva.ValidarSolapamiento()`, `Reserva.CrearReserva()` | Reserva, Espacio | `FrmDisponibilidad`, `FrmConfirmacion` | test_04, test_05 | Implementado |
| RF-05 | CU-04 (FA-01 / EX-02) | CtrlCrearReserva | `Config/AppConfig.cs` · `PermisosCategoria`; `Models/Usuario.cs` | `AppConfig.ObtenerRolesPermitidos()`, `Usuario.VerificarRol()` | Usuario, Espacio | `FrmDisponibilidad` | test_06 | Implementado |
| RF-07 | CU-04 (FA-02 / EX-03) | CtrlCrearReserva | `Config/AppConfig.cs` · `LimitesSemanalesHoras`; `Models/Reserva.cs` | `AppConfig.ObtenerLimiteSemanal()`, `Reserva.CalcularHorasSemanales()` | Reserva, Usuario | `FrmDisponibilidad` | test_07 | Implementado |
| RF-08 | CU-04 (EX-04) | Servicio de Correo | `Models/ServicioCorreo.cs` · `ServicioCorreo`, `ErrorServicioCorreo` | `EnviarConfirmacion()` | N/A (log) | `FrmConfirmacion` | test_08 | Implementado (SMTP simulado) |
| RF-11 (parcial) | CU-09 Mis reservas | Controlador de Consultas | `Models/Reserva.cs` | `ObtenerMisReservas()` | Reserva, Usuario | `FrmMisReservas` | — | Apoyo a evidencia |
| RNF-01 | — | — | `Controllers/CtrlCrearReserva.cs` | `Stopwatch` en `ProcesarReserva()` | — | `FrmConfirmacion` | test_04 | Verificado |
| RNF-03 | — | — | `Models/Db.cs` · `Seguridad` | `HashPassword()`, `VerifyPassword()` | Usuario | — | test_01 | Verificado |
| RF-06, RF-09, RF-10, RF-12 | CU-05..CU-08 | — | — | — | — | — | — | Fuera del alcance de Fase 3 |

---

## 6. Registro de ajustes justificados respecto al diseño (Fase 2)
1. **Cliente web → cliente de escritorio (Windows Forms).** Se mantiene la arquitectura Cliente-Servidor + MVC: el servidor es SQL Server y el cliente separa Vistas (`Views/`), Controladores (`Controllers/`) y Modelo (`Models/`). RNF-02 (compatibilidad navegador/móvil) queda acotado a Windows en el prototipo.
2. **Sesión en memoria del cliente.** Al ser una aplicación de escritorio de un usuario, `CtrlAuth.UsuarioActual` sustituye a la sesión por token/cookie.
3. **Concurrencia con SQL Server.** `BEGIN IMMEDIATE` (SQLite) se reemplaza por transacción `Serializable` con `UPDLOCK, HOLDLOCK`, que bloquea el rango consultado y evita reservas duplicadas simultáneas.
4. **Ocupación por solapamiento.** Una reserva de varias horas marca como *Ocupado* todos los bloques que cubre (antes solo el bloque exacto).
5. **Validación adicional.** Las reservas deben estar dentro de la franja 08:00-18:00 y la BD tiene `CHECK (hora_fin > hora_inicio)`.
6. **Correo simulado.** RF-08 se simula escribiendo en `logs\notificaciones_correo.log` (junto al .exe); EX-04 se demuestra con la casilla de fallo SMTP.
