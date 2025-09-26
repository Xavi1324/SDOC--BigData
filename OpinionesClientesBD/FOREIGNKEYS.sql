
USE OpinionesClientesDW;
GO

ALTER TABLE dw.Producto
  ADD CONSTRAINT FK_Producto_Categoria
  FOREIGN KEY (Categoria_Id) REFERENCES dw.Categoria(Categoria_Id);

ALTER TABLE dw.Cliente
  ADD CONSTRAINT FK_Cliente_Pais
  FOREIGN KEY (Pais_Id) REFERENCES dw.Pais(Pais_Id);

ALTER TABLE dw.Fuente
  ADD CONSTRAINT FK_Fuente_Tipo
  FOREIGN KEY (TipoFuente_Id) REFERENCES dw.TipoFuente(TipoFuente_Id);

ALTER TABLE dw.Opinion
  ADD CONSTRAINT FK_Opinion_Producto
  FOREIGN KEY (Product_Id) REFERENCES dw.Producto(Product_Id);

ALTER TABLE dw.Opinion
  ADD CONSTRAINT FK_Opinion_Cliente
  FOREIGN KEY (Client_Id) REFERENCES dw.Cliente(Client_Id);

ALTER TABLE dw.Opinion
  ADD CONSTRAINT FK_Opinion_Fuente
  FOREIGN KEY (Fuente_Id) REFERENCES dw.Fuente(Fuente_Id);

ALTER TABLE dw.Opinion
  ADD CONSTRAINT FK_Opinion_Tiempo
  FOREIGN KEY (Time_Id) REFERENCES dw.Tiempo(Time_Id);

ALTER TABLE dw.Opinion
  ADD CONSTRAINT FK_Opinion_Clasificacion
  FOREIGN KEY (Class_Id) REFERENCES dw.Clasificacion(Class_Id);
GO