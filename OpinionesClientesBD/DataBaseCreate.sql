CREATE DATABASE OpinionesClientesDW;
GO
USE OpinionesClientesDW;
GO


IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dw')
  EXEC('CREATE SCHEMA dw AUTHORIZATION dbo;');
GO



CREATE TABLE dw.Categoria (
  Categoria_Id INT IDENTITY(1,1) PRIMARY KEY,
  Nombre       VARCHAR(100) NOT NULL
);

CREATE TABLE dw.Pais (
  Pais_Id INT IDENTITY(1,1) PRIMARY KEY,
  Nombre  VARCHAR(100) NOT NULL
);

CREATE TABLE dw.TipoFuente (
  TipoFuente_Id INT IDENTITY(1,1) PRIMARY KEY,
  Descripcion   VARCHAR(100) NOT NULL
);

CREATE TABLE dw.Clasificacion (
  Class_Id SMALLINT IDENTITY(1,1) PRIMARY KEY,
  Code     CHAR(3)     NOT NULL,   
  Nombre   VARCHAR(50) NOT NULL
);

CREATE TABLE dw.Producto (
  Product_Id   INT IDENTITY(1,1) PRIMARY KEY,
  ProductName  VARCHAR(150) NOT NULL,
  Categoria_Id INT NOT NULL
);

CREATE TABLE dw.Cliente (
  Client_Id  INT IDENTITY(1,1) PRIMARY KEY,
  ClientName VARCHAR(100) NOT NULL,
  LastName   VARCHAR(100) NOT NULL,
  Email      VARCHAR(200) NULL,
  Pais_Id    INT NULL
);

CREATE TABLE dw.Tiempo (
  Time_Id INT IDENTITY(1,1) PRIMARY KEY,
  [Date]  DATE    NOT NULL UNIQUE,
  [Year]  INT     NOT NULL,
  [Month] TINYINT NOT NULL,
  [Day]   TINYINT NOT NULL,
  [Hour]  TINYINT NULL,
  DateKey AS ([Year]*10000 + [Month]*100 + [Day]) PERSISTED
);

CREATE TABLE dw.Fuente (
  Fuente_Id     INT IDENTITY(1,1) PRIMARY KEY,
  TipoFuente_Id INT NOT NULL,
  Nombre        VARCHAR(100) NOT NULL,
  UrlPath       VARCHAR(300) NULL,
  FechaRegistro DATE NOT NULL
);



CREATE TABLE dw.Opinion (
  Opinion_Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  Product_Id INT     NOT NULL,
  Client_Id  INT     NULL,
  Fuente_Id  INT     NOT NULL,
  Time_Id    INT     NOT NULL,
  [Comment]  NVARCHAR(MAX) NOT NULL,
  Class_Id   SMALLINT NOT NULL,
  HashUnique CHAR(64) NOT NULL UNIQUE
);
