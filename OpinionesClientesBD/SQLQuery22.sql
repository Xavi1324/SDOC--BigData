USE OpinionesClientesDW;
GO

CREATE OR ALTER VIEW dw.vw_WebReviews AS
SELECT 
    o.Opinion_Id   AS OpinionId,
    o.Product_Id   AS ProductId,
    o.Client_Id    AS ClientId,
    o.Fuente_Id    AS FuenteId,
    o.Time_Id      AS TimeId,
    o.Comment      AS Comment,
    o.Class_Id     AS ClassId
FROM dw.Opinion AS o
INNER JOIN dw.Fuente AS f 
    ON o.Fuente_Id = f.Fuente_Id
WHERE f.Nombre = 'WebReviews';
GO


USE OpinionesClientesDW;
GO

CREATE OR ALTER VIEW dw.vw_SocialCommetsApi AS
SELECT 
    o.Opinion_Id   AS Id,
    o.Client_Id    AS IdClient,
    o.Product_Id   AS IdProduct,
    f.Nombre       AS Source,
    o.Comment      AS Comment
FROM dw.Opinion AS o
INNER JOIN dw.Fuente AS f 
    ON o.Fuente_Id = f.Fuente_Id
INNER JOIN dw.TipoFuente AS tf
    ON f.TipoFuente_Id = tf.TipoFuente_Id
WHERE tf.Descripcion = 'Red Social';
GO
