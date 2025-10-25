CREATE DATABASE SDOC_OLAP;
GO 

USE SDOC_OLAP;
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'olap')
  EXEC('CREATE SCHEMA olap AUTHORIZATION dbo;');
GO


-- =============================================
-- DIMENSION TABLES
-- =============================================

-- Dimensión Tiempo (enriquecida)
CREATE TABLE olap.Dim_Tiempo (
  Time_SK INT PRIMARY KEY,         
  [Date] DATE NOT NULL,
  [Year] INT NOT NULL,
  [Month] TINYINT NOT NULL,
  MonthName VARCHAR(20) NOT NULL,
  [Day] TINYINT NOT NULL,
  DayName VARCHAR(20) NOT NULL,
  Quarter TINYINT NOT NULL,
  WeekOfYear TINYINT NOT NULL
);

-- Dimensión Categoría (parte del Copo de Nieve)
CREATE TABLE olap.Dim_Categoria (
    Categoria_SK INT IDENTITY(1,1) PRIMARY KEY, -- Surrogate Key
    Nombre VARCHAR(100) NOT NULL
);

-- Dimensión Producto (se conecta a Categoria, creando el Copo de Nieve)
CREATE TABLE olap.Dim_Producto (
  Product_SK INT IDENTITY(1,1) PRIMARY KEY,    -- Surrogate Key
  ProductName VARCHAR(150) NOT NULL,
  Categoria_SK INT NOT NULL,
  FOREIGN KEY (Categoria_SK) REFERENCES olap.Dim_Categoria(Categoria_SK)
);

-- Dimensión Cliente (Modelo Estrella, incluye País)
CREATE TABLE olap.Dim_Cliente (
  Client_SK INT IDENTITY(1,1) PRIMARY KEY,     -- Surrogate Key
  ClientName VARCHAR(100) NOT NULL,
  LastName VARCHAR(100) NOT NULL,
  Email VARCHAR(200) NULL,
  PaisNombre VARCHAR(100) NULL
);

-- Dimensión Fuente (Modelo Estrella, incluye TipoFuente)
CREATE TABLE olap.Dim_Fuente (
  Fuente_SK INT IDENTITY(1,1) PRIMARY KEY,      -- Surrogate Key
  NombreFuente VARCHAR(100) NOT NULL,
  TipoFuenteDesc VARCHAR(100) NOT NULL
);

-- Dimensión Clasificación
CREATE TABLE olap.Dim_Clasificacion (
  Class_SK SMALLINT IDENTITY(1,1) PRIMARY KEY, -- Surrogate Key
  Class_Code CHAR(3) NOT NULL,
  Class_Nombre VARCHAR(50) NOT NULL
);

CREATE TABLE olap.Fact_Opiniones (
  Opinion_PK BIGINT IDENTITY(1,1) PRIMARY KEY,
  Time_SK INT NOT NULL,
  Product_SK INT NOT NULL,
  Client_SK INT NOT NULL,
  Fuente_SK INT NOT NULL,
  Class_SK SMALLINT NOT NULL,
  TotalComentarios TINYINT NOT NULL DEFAULT 1,
  PuntajeSatisfaccion SMALLINT NOT NULL,
  FOREIGN KEY (Time_SK) REFERENCES olap.Dim_Tiempo(Time_SK),
  FOREIGN KEY (Product_SK) REFERENCES olap.Dim_Producto(Product_SK),
  FOREIGN KEY (Client_SK) REFERENCES olap.Dim_Cliente(Client_SK),
  FOREIGN KEY (Fuente_SK) REFERENCES olap.Dim_Fuente(Fuente_SK),
  FOREIGN KEY (Class_SK) REFERENCES olap.Dim_Clasificacion(Class_SK)
);
GO