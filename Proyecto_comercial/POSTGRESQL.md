# Actualización del proyecto existente MenuCompleto

## Estado de entrega

Se mantiene ASP.NET Web Forms, C# y .NET Framework 4.7.2. No se creó otra solución. El respaldo previo está en `../../respaldos/MenuCompleto_antes_PostgreSQL.zip`, relativo a esta carpeta. El proyecto conserva Login, Registro, Inicio, Empresa, PlanCuentas y la guía descriptiva original.

La conexión y los CRUD están preparados para PostgreSQL mediante Npgsql 4.1.14 y consultas parametrizadas. **La instalación de PostgreSQL, la ejecución de los scripts y la prueba de persistencia siguen pendientes en tu equipo.** No se ha supuesto que exista la base de datos ni se han inventado credenciales. Por ahora `Almacenamiento=Memoria` mantiene funcionando el avance anterior. Se muestra ese modo en todas las páginas; no hay cambio automático a memoria si PostgreSQL falla.

## 1. Comprobar el servidor

pgAdmin 4 es el cliente de administración, no el servidor. Abrir pgAdmin y comprobar si existe un servidor local conectado. Si no existe, verificar en Aplicaciones instaladas y en Servicios de Windows si aparece PostgreSQL. Si no está instalado, instalar el servidor PostgreSQL desde su distribución oficial para Windows, incluyendo el componente servidor y las herramientas; conservar la contraseña que elijas para `postgres`. La instalación del servidor no se realizó automáticamente.

En pgAdmin: clic derecho en **Servers → Register → Server**. Nombre descriptivo: `PostgreSQL local`. En **Connection**:

- Host: `localhost`
- Puerto: `5432`
- Maintenance database: `postgres`
- Username: `postgres`
- Password: la contraseña local que configuraste al instalar PostgreSQL.

Si no conecta, comprobar que el servicio esté iniciado y que el puerto coincida con la instalación. No hace falta enviar la contraseña por el chat.

## 2. Crear la base y las tablas desde pgAdmin

Los scripts están dentro del proyecto, en `MenuCompleto/Sql`.

1. Seleccionar la base **postgres** y abrir **Query Tool**. Abrir `01_crear_base.sql` y ejecutar con autocommit. `CREATE DATABASE` no puede ejecutarse dentro de una transacción. Si ya existe `bd_sistema_contable`, omitir este primer script: no borres la base.
2. Refrescar **Databases**. Seleccionar **bd_sistema_contable** y abrir un **nuevo** Query Tool. Verificar en la conexión superior que apunta a esta base, no a `postgres`.
3. Abrir y ejecutar `02_esquema.sql`. Debe ejecutarse una sola vez sobre una base vacía. Incluye tablas, índices, roles, claves y validaciones. Usa `btree_gist` para impedir períodos de IVA superpuestos, por lo que debe ejecutarse con un usuario autorizado para crear esa extensión (el usuario local `postgres` normalmente lo permite). Requiere PostgreSQL 12 o posterior por las columnas generadas.
4. Ejecutar `03_datos_carchi.sql` una sola vez. Carga la empresa del caso y las 91 cuentas cotejadas. No inserta usuarios ni contraseñas. No carga tarifas con fechas inventadas.
5. Comprobar desde Query Tool:

```sql
SELECT current_database();
SELECT count(*) FROM plan_cuentas; -- 91 al completar la carga inicial sin modificaciones
SELECT sum(debe_inicial),sum(haber_inicial) FROM plan_cuentas;
-- 143065.00 / 143065.00
SELECT codigo_cuenta,nombre_cuenta,debe_inicial,haber_inicial
FROM plan_cuentas ORDER BY codigo_cuenta;
```

Los scripts no eliminan tablas ni utilizan `ON DELETE CASCADE`. No volver a ejecutarlos sobre información ingresada. Un error revierte el bloque transaccional correspondiente; revisar la causa antes de repetirlo. Este procedimiento crea una base nueva; no exporta automáticamente los datos que se hubieran digitado en el modo temporal anterior.

## 3. Configurar la conexión sin contraseñas en el código fuente

En `MenuCompleto` copiar **Conexion.local.config.example** como **Conexion.local.config**. Este archivo está excluido en `.gitignore`; los archivos `.config` no se sirven como contenido web por ASP.NET/IIS. No incluirlo en repositorios ni compartirlo con la contraseña.

Editar **solo la copia local**, reemplazando el marcador de contraseña. Si la contraseña contiene caracteres reservados XML, escaparlos (`&` → `&amp;`, comillas → `&quot;`). Para caracteres especiales de la cadena Npgsql usar también el entrecomillado apropiado de la contraseña dentro de la cadena.

En `Web.config`, reemplazar la sección `connectionStrings` completa por:

```xml
<connectionStrings configSource="Conexion.local.config" />
```

Después de crear correctamente las tablas, cambiar el valor existente de configuración:

```xml
<appSettings>
  <add key="Almacenamiento" value="PostgreSQL" />
</appSettings>
```

No agregar una segunda sección `appSettings`: editar la existente. Reiniciar IIS Express desde Visual Studio. También se admite la variable de entorno del proceso **CARCHI_POSTGRES_CONNECTION**, que tiene prioridad sobre `connectionStrings`; no escribir su valor en comandos compartidos ni en registros. Si no se usa esa variable, basta con la copia local anterior.

Host, puerto, usuario y base predeterminados: `localhost`, `5432`, `postgres`, `bd_sistema_contable`. El archivo versionado `Web.config` no contiene una contraseña y permanece en modo Memoria para que el proyecto pueda abrirse antes de instalar/configurar PostgreSQL.

Para una instalación compartida se debe crear un usuario de aplicación con permisos limitados en lugar de usar la cuenta administrativa del servidor; la configuración local solicitada utiliza `postgres` para facilitar las primeras pruebas.

## 4. Ejecutar y comprobar los CRUD reales

1. Abrir la solución existente en Visual Studio. Restaurar los paquetes indicados en `packages.config`. La carpeta `packages` ya contiene Npgsql y sus dependencias. Establecer `Login.aspx` como inicio.
2. Ejecutar desde localhost. La nota inferior debe indicar **PostgreSQL · almacenamiento persistente**.
3. En Registro crear el primer administrador local, con contraseña de 10 a 128 caracteres y confirmación. Se guarda PBKDF2 SHA-256 con sal, nunca contraseña en texto plano. El registro público posterior solo asigna rol Usuario.
4. Iniciar sesión y crear un usuario de prueba. Seleccionarlo, cambiar correo/nombres y actualizar. Eliminarlo con confirmación. Probar duplicado de nombre y protección del último administrador activo, incluida desactivación o cambio de rol.
5. Crear una empresa sin RUC real inventado. Seleccionarla, editar y actualizar. Eliminar una empresa vacía. Una empresa con cuentas o tarifas debe rechazar la eliminación.
6. Empresa usa pestañas para datos generales y configuración tributaria. Seleccionar primero la empresa en la tabla; pasar a la pestaña tributaria. Crear, seleccionar, editar y eliminar una tarifa de prueba. Intentar otra que se superponga: debe rechazarse. No usar una tarifa oficial asumida; el porcentaje y las fechas son configuración del administrador.
7. En Plan de Cuentas seleccionar empresa. Crear una cuenta Grupo y una hija Movimiento usando códigos válidos. Editar el nombre/saldo de la hija y actualizar. No se puede eliminar el padre mientras tenga hijos, ni ingresar códigos duplicados o saldos negativos. Búsqueda por código/nombre y filtro por grupo se conservan.
8. Iniciar sesión como Usuario: consultar empresas, cuentas y tarifas. Los servicios comprueban el rol en cada escritura; los botones también se deshabilitan. La navegación de usuarios se adapta al rol.
9. Cerrar sesión y volver a entrar: los registros deben seguir disponibles. Detener **todo IIS Express**, volver a ejecutarlo y consultar de nuevo: los cambios deben permanecer en PostgreSQL. Confirmar también con SELECT desde pgAdmin. Este último paso es la prueba de persistencia y aún no pudo ejecutarse aquí porque no hay servidor/base configurados.

Para cambiar a modo Memoria se debe editar la configuración explícitamente; los datos SQL no se borran. No hay importación automática desde memoria a SQL ni sincronización entre ambos modos.

## Fuentes contables e IVA

Se leyó el catálogo del PDF, páginas impresas 459–461, y la hoja **Plan de Cuentas** del Excel. Se cotejaron los valores almacenados de Debe/Haber con sus celdas referidas de **Transacciones**: 44 cuentas operativas coinciden, y los saldos iniciales equilibran en 143065.00 por lado. Las otras cuentas del catálogo conservan saldo cero. No se recalculó ni modificó el Excel. El cotejo se guarda en `../../revision/cotejo_cuentas.csv`, relativo a esta carpeta.

Los códigos se conservan. Para cuentas presentes en ambas fuentes se utiliza el nombre operativo del Excel; la jerarquía procede del catálogo del PDF. Se conserva la clasificación AC/ANC/PC/PNC/PAT/ING/GAD/GVE/GFI/COS/TRA cuando existe en el Excel. Los agrupadores no aceptan movimientos; las contracuentas de activo tienen naturaleza acreedora. Las cuentas tributarias del catálogo se conservan como catálogo, sin implementar ni activar retenciones.

El documento usa año **200X**, que no determina una fecha calendario. Por ello no se convierten las tarifas de referencia del ejercicio en una configuración fechada ficticia. El IVA permanece editable, por empresa y vigencia, sin porcentaje fijo. Las operaciones futuras conservarán el resultado inmutable con su porcentaje aplicado. La implementación actual admite una línea temporal por empresa y tratamientos explícitos gravado, tarifa cero, exento y no objeto; varias categorías simultáneas requerirán ampliar la clave de configuración. No se desarrollaron Transacciones, Diario, Mayor ni otros módulos contables.

## Implementación y archivos modificados

- `Datos/UnidadPostgres.cs`: conexiones centralizadas, parámetros, transacciones con rollback ante errores, lectura SQL y escritura de diferencias. Los servicios existentes reutilizan sus validaciones en una unidad de trabajo; sus colecciones son la representación temporal de esa operación, **no el almacenamiento** cuando PostgreSQL está activo. Cada operación vuelve a leer la base. Solo se insertan/actualizan/eliminan filas modificadas, sin vaciar tablas ni volver a sembrarlas.
- `Datos/DatosTemporales.cs`: selección explícita del modo, catálogo cotejado para la demostración y asignación de IDs mediante secuencias cuando se usa PostgreSQL.
- `Servicios/UsuarioService.cs`, `EmpresaService.cs`, `PlanCuentasService.cs`, `TarifaIVAService.cs`: límites transaccionales reutilizando permisos y reglas ya existentes. Contraseñas, roles, historial y dependencias se conservan al guardar.
- SQL: tablas relacionales `usuarios`, `roles`, `usuario_roles`, `empresas`, `plan_cuentas`, `tarifas_iva`; FK restrictivas, unicidad, NUMERIC, jerarquía y protección de tarifas utilizadas. Se utiliza un rol por usuario actualmente, respetando el modelo de acceso existente.
- Las operaciones usan un cerrojo local y un bloqueo transaccional de PostgreSQL compartido con los triggers, para mantener coherencia incluso entre varios procesos web. Esto serializa el mantenimiento y carga catálogos completos; es una adaptación para el volumen universitario actual. Para grandes volúmenes conviene sustituir el adaptador por consultas por entidad y paginación, conservando las interfaces de servicios.
- `Sitio.Master`, `.cs`, `sistema.css`, `sistema.js`: plantilla común, navegación por rol, login centrado, pestañas accesibles con teclado, botones diferenciados, foco visible y tablas adaptables. Bootstrap 5.3.3 se incluye localmente en `bootstrap.min.css`; los estilos del proyecto completan el diseño sin exigir Internet.
- `Empresa.aspx` y `PlanCuentas.aspx`: organización tributaria mediante pestañas y notas del catálogo actualizado. Rutas y eventos existentes conservados.
- `Web.config`, `packages.config`, `MenuCompleto.csproj`: proveedor y referencias de Npgsql, dependencias, redirecciones verificadas, configuración sin secretos. Npgsql 4.x se eligió para conservar la compatibilidad del proyecto heredado; ver [notas oficiales de compatibilidad](https://www.npgsql.org/doc/compatibility.html).
- `Conexion.local.config.example` y `.gitignore`: plantilla y exclusión de credenciales locales.
- `Global.asax` y `EstadoConexion.aspx`: pantalla de configuración pendiente ante fallos de almacenamiento, sin mostrar cadenas de conexión ni contraseñas.
- `Pruebas/Verificar.ps1`: comprueba compilación y páginas, ahora incluyendo las referencias de Npgsql. Las pruebas de servicios se ejecutan sin configuración PostgreSQL y por tanto validan el modo Memoria. Las pruebas SQL reales requieren servidor y una base de pruebas aparte.

La guía `PRIMER_AVANCE.md` describe el estado anterior y se conserva como antecedente; para esta actualización prevalece este documento. No se reparó ni alteró la instalación local de MSBuild de Visual Studio: se utiliza el script de verificación con Roslyn y las referencias de .NET Framework 4.7.2 cuando MSBuild presenta el conflicto de System.Memory ya detectado.

## Comprobaciones realizadas

- Compilación C# contra .NET Framework 4.7.2 y precompilación de las páginas ASP.NET, con las dependencias Npgsql instaladas.
- 42 comprobaciones de servicios correctas en modo Memoria.
- 21 comprobaciones HTTP correctas contra IIS Express en modo Memoria, incluyendo autenticación, roles, carga de páginas e inserción de empresas, tarifas y cuentas.
- Cotejo de 44 saldos operativos con las celdas fuente del Excel y carga de 91 códigos del catálogo PDF.
- **Pendientes:** ejecutar los scripts en PostgreSQL, probar los CRUD SQL y verificar permanencia después de reiniciar IIS Express. No se reportan como pruebas aprobadas. La captura visual tampoco pudo completarse: el navegador integrado agotó su tiempo de conexión; las páginas sí respondieron por HTTP.

La instancia de IIS Express usada para las pruebas se detuvo al terminar para descartar los datos ficticios de memoria. El proyecto queda en modo Memoria hasta que completes los pasos de PostgreSQL.
