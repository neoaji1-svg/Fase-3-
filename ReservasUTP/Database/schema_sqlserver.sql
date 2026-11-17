-- =====================================================================
-- Esquema relacional (SQL Server): Sistema de Gestión de Reservas UTP
-- Coherente con Fase 2 (Modelo de Datos). Idempotente: se puede
-- ejecutar varias veces. La aplicación lo ejecuta automáticamente.
-- Para ejecución manual en SSMS: crear antes la BD ReservasUTP.
-- =====================================================================

IF OBJECT_ID(N'dbo.Usuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuario (
        id_usuario     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuario PRIMARY KEY,
        cedula         NVARCHAR(20)  NOT NULL CONSTRAINT UQ_Usuario_Cedula UNIQUE,
        nombre         NVARCHAR(100) NOT NULL,
        correo         NVARCHAR(100) NOT NULL CONSTRAINT UQ_Usuario_Correo UNIQUE,
        password_hash  NVARCHAR(255) NOT NULL,
        rol            NVARCHAR(30)  NOT NULL CONSTRAINT CK_Usuario_Rol CHECK (
            rol IN (N'Estudiante Regular', N'Estudiante Tesista', N'Docente', N'Administrativo', N'Administrador')
        )
    );
END
GO

IF OBJECT_ID(N'dbo.Espacio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Espacio (
        id_espacio    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Espacio PRIMARY KEY,
        nombre        NVARCHAR(100) NOT NULL,
        categoria     NVARCHAR(50)  NOT NULL,
        capacidad     INT NOT NULL CONSTRAINT CK_Espacio_Capacidad CHECK (capacidad > 0),
        equipamiento  NVARCHAR(500) NULL,
        estado        NVARCHAR(20)  NOT NULL CONSTRAINT DF_Espacio_Estado DEFAULT N'Habilitado'
                      CONSTRAINT CK_Espacio_Estado CHECK (estado IN (N'Habilitado', N'Inhabilitado'))
    );
END
GO

IF OBJECT_ID(N'dbo.Reserva', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reserva (
        id_reserva      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reserva PRIMARY KEY,
        id_usuario      INT NOT NULL,
        id_espacio      INT NOT NULL,
        fecha           DATE NOT NULL,
        hora_inicio     TIME(0) NOT NULL,
        hora_fin        TIME(0) NOT NULL,
        fecha_registro  DATETIME2(0) NOT NULL CONSTRAINT DF_Reserva_FechaRegistro DEFAULT SYSDATETIME(),
        estado          NVARCHAR(20) NOT NULL CONSTRAINT DF_Reserva_Estado DEFAULT N'Activa'
                        CONSTRAINT CK_Reserva_Estado CHECK (estado IN (N'Activa', N'Cancelada')),
        CONSTRAINT CK_Reserva_Horas CHECK (hora_fin > hora_inicio),
        CONSTRAINT FK_Reserva_Usuario FOREIGN KEY (id_usuario) REFERENCES dbo.Usuario(id_usuario),
        CONSTRAINT FK_Reserva_Espacio FOREIGN KEY (id_espacio) REFERENCES dbo.Espacio(id_espacio)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reserva_Espacio_Fecha')
    CREATE INDEX IX_Reserva_Espacio_Fecha ON dbo.Reserva(id_espacio, fecha, estado);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reserva_Usuario_Fecha')
    CREATE INDEX IX_Reserva_Usuario_Fecha ON dbo.Reserva(id_usuario, fecha, estado);
GO
