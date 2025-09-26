USE OpinionesClientesDW;
GO

ALTER TABLE dw.Categoria     ADD CONSTRAINT UQ_Categoria_Nombre   UNIQUE (Nombre);
ALTER TABLE dw.Pais          ADD CONSTRAINT UQ_Pais_Nombre        UNIQUE (Nombre);
ALTER TABLE dw.TipoFuente    ADD CONSTRAINT UQ_TipoFuente_Desc    UNIQUE (Descripcion);
ALTER TABLE dw.Clasificacion ADD CONSTRAINT UQ_Clasificacion_Code UNIQUE (Code);

CREATE UNIQUE INDEX UQ_Producto_NombreCat
  ON dw.Producto(ProductName, Categoria_Id);

CREATE UNIQUE INDEX UQ_Cliente_Email_NotNull
  ON dw.Cliente(Email)
  WHERE Email IS NOT NULL;

ALTER TABLE dw.Tiempo
  ADD CONSTRAINT CK_Tiempo_Month CHECK ([Month] BETWEEN 1 AND 12),
      CONSTRAINT CK_Tiempo_Day   CHECK ([Day]   BETWEEN 1 AND 31),
      CONSTRAINT CK_Tiempo_Hour  CHECK ([Hour] IS NULL OR [Hour] BETWEEN 0 AND 23);

CREATE INDEX IX_Opinion_Producto_Fecha ON dw.Opinion(Product_Id, Time_Id);
CREATE INDEX IX_Opinion_Fuente_Fecha   ON dw.Opinion(Fuente_Id,  Time_Id);
CREATE INDEX IX_Opinion_Cliente_Fecha  ON dw.Opinion(Client_Id,  Time_Id);
CREATE INDEX IX_Opinion_Class_Fecha    ON dw.Opinion(Class_Id,   Time_Id);


GO
