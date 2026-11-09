# Primer avance funcional

Se conserva ASP.NET Web Forms, C# y .NET Framework 4.7.2. No se utiliza base de datos, Entity Framework ni SQL. Las colecciones pertenecen al proceso de la aplicación y se pierden al reiniciar, recompilar o reciclar IIS. En varios procesos IIS cada proceso tendría su propia colección; esta demostración debe ejecutarse con un solo proceso.

## Ejecutar en Visual Studio

1. Abrir `MenuCompleto.slnx` con Visual Studio o directamente `MenuCompleto/MenuCompleto.csproj`.
2. Tener instalada la carga de trabajo de desarrollo ASP.NET, IIS Express y el Developer Pack de .NET Framework 4.7.2. Restaurar los paquetes NuGet existentes del proyecto si Visual Studio lo solicita.
3. Establecer `Login.aspx` como página de inicio y ejecutar con IIS Express. También se configuró como documento predeterminado.
4. Desde **localhost**, abrir **Registrarse / Configuración inicial**. Si no existe ninguna cuenta, el registro crea al primer administrador. Ingresar nombres, apellidos, usuario, correo y contraseña de entre 10 y 128 caracteres, con su confirmación. No existe contraseña predeterminada. La configuración inicial está bloqueada en solicitudes remotas y se realiza una sola vez por proceso.
5. Volver a `Login.aspx` e iniciar sesión. `Inicio.aspx` es el menú funcional con barra lateral. El enlace **Guía del proyecto** conserva el menú descriptivo original `menu.aspx` y sus páginas de referencia.

## Archivos

- Nuevas páginas completas: `Login.aspx`, `Registro.aspx`, `Empresa.aspx`, `PlanCuentas.aspx`, `Inicio.aspx`, cada una con code-behind y designer.
- `Sitio.Master`, sus archivos de código y `sistema.css`: navegación común, identidad, cierre de sesión y diseño adaptable.
- `Modelos`: usuarios, roles, empresas, cuentas, tarifas y resultado inmutable del cálculo tributario.
- `Datos/DatosTemporales.cs`: colecciones compartidas y datos iniciales parciales. Un único bloqueo protege también las validaciones entre colecciones.
- `Servicios`: autenticación, usuarios, empresas, cuentas, tarifas y validaciones. Las lecturas devuelven copias; las escrituras vuelven a comprobar el usuario activo y su rol en el servidor.
- `Infraestructura/PaginaBase.cs`: protección de páginas, consulta actual del usuario, ViewState ligado a sesión y token contra solicitudes de modificación externas, mensajes y conversión de fechas/importes.
- Modificados `MenuCompleto.csproj` y `Web.config` para registrar los archivos y configurar el inicio/sesión. `menu.aspx`, `menu.aspx.cs` y `modulo.aspx.cs` conservan la guía y añaden navegación al sistema y autenticación.
- `Pruebas` en la raíz del espacio de trabajo: pruebas automatizadas de servicios, pruebas HTTP y script de compilación/verificación.

## Probar las funcionalidades

### Usuarios y acceso

- Como administrador, en **Gestión de usuarios**, completar campos y **Guardar**. **Seleccionar** en la tabla carga los datos. Cambiar nombres/correo/rol/estado y pulsar **Actualizar**. Una contraseña vacía al editar conserva la existente; si se cambia debe confirmarse. **Eliminar** solicita confirmación. **Buscar** filtra por nombre o usuario; **Nuevo** y **Limpiar** limpian la selección.
- Intentar repetir un nombre de usuario (sin distinguir mayúsculas), ingresar correo inválido o contraseñas distintas. Se debe mostrar el error sin alterar los registros.
- No es posible eliminar, desactivar o quitar el rol al último administrador activo.
- Cerrar sesión y registrar una cuenta pública. Siempre recibe rol Usuario, aunque se intente enviar otro rol. Iniciar sesión con ella: puede consultar empresas, tarifas y cuentas, pero no administrar usuarios ni modificar datos. Los servicios comprueban permisos incluso ante POST construido manualmente.
- El listado nunca contiene hashes, sales ni contraseñas. Se usa PBKDF2 SHA-256 con sal aleatoria de 24 bytes y 120000 iteraciones. Cinco fallos de login bloquean los intentos durante un minuto en esa sesión; es una protección básica para esta demostración, no un limitador distribuido de producción.
- **Cerrar sesión** es un POST que abandona la sesión. Una cuenta desactivada o eliminada pierde acceso al realizar otra solicitud.

### Empresas

- **Guardar** inserta una empresa; el ID se genera automáticamente. Razón social y fecha son obligatorias. El RUC es opcional para no inventar información oficial; si se proporciona se comprueban longitud, provincia, tipo, dígito verificador y establecimiento. El correo se valida si se proporciona.
- **Seleccionar** carga todos los campos y habilita la asociación de tarifas. Editar y **Actualizar**; buscar por RUC/razón social. **Eliminar** solo funciona sin cuentas ni tarifas relacionadas.
- Datos oficiales desconocidos de Carchi se dejan vacíos. La moneda es USD.

### IVA dinámico

- Seleccionar una empresa en la tabla. En **Configuración tributaria**, ingresar porcentaje, inicio, fin opcional y descripción. **Insertar tarifa**, **Seleccionar**, **Actualizar tarifa** y **Eliminar tarifa** operan sobre el servicio compartido. El ID no se ingresa manualmente; la empresa se obtiene de la selección superior.
- Prueba didáctica (sin afirmar vigencia tributaria oficial): registrar 12 % del 2020-01-01 al 2020-12-31 y 15 % desde el 2021-01-01. Rechazar un período que incluya días de ambos. Los extremos son inclusivos y las fechas se interpretan por día, sin hora. Se permiten huecos, pero calcular en ellos produce un error; no hay tarifa de respaldo.
- Estado **Programado / Vigente / Histórico** se deriva de las fechas respecto del día del servidor. No se edita manualmente para evitar estados contradictorios.
- Para programar un cambio, cerrar primero la tarifa anterior el día previo. Si ya fue utilizada, se permite cerrar su período siempre que no se excluya ninguna fecha de uso confirmada; empresa, porcentaje e inicio permanecen protegidos. No se elimina una tarifa utilizada.
- `ObtenerAplicable(actor, empresa, fecha)` obtiene la tarifa configurada. `Calcular(actor, empresa, fecha, subtotal, tratamiento)` obtiene el porcentaje y devuelve una instantánea inmutable con ID de tarifa, fecha, empresa, tratamiento, porcentaje aplicado, subtotal, IVA y total. No se implementan transacciones en esta etapa.
- `Calcular` es una previsualización. La futura operación debe conservar la instantánea y llamar a `ConfirmarUso` al registrarla; este método comprueba que la tarifa no cambió y protege su historial. La futura persistencia deberá hacer ambas acciones dentro de una misma unidad de trabajo.
- Importes y porcentajes son `decimal`. Se redondea subtotal a dos decimales y luego el impuesto a dos decimales con `MidpointRounding.AwayFromZero`. El total suma esos importes redondeados. Para 1000.00: 12 % produce IVA 120.00/total 1120.00; 15 % produce 150.00/1150.00.
- Tratamientos preparados: Gravado, TarifaCero, NoObjeto y Exento. Los dos últimos no buscan tarifa y conservan tratamiento explícito. TarifaCero requiere una configuración de 0 %. Actualmente hay una sola línea temporal por empresa: soportar varias categorías simultáneas requerirá agregar un identificador de configuración/categoría a la tarifa y a la búsqueda. No se crean automáticamente operaciones gravadas ni tarifas oficiales.

### Plan de cuentas

- Seleccionar empresa, ingresar código jerárquico, nombre, grupo 1–6, padre inmediato, naturaleza, tipo, clasificación Excel opcional y saldos. Usar punto decimal sin separadores de miles, por ejemplo `5500.00`.
- Crear primero una cuenta **Grupo** y luego sus descendientes. Ejemplo: `1`, `1.1`, `1.1.6`. La cuenta padre debe existir en la misma empresa y ser Grupo. El nivel se calcula a partir del código. El prefijo determina el grupo y evita ciclos.
- **Seleccionar** carga la cuenta; **Actualizar** modifica y **Eliminar** exige confirmación. Las cuentas con hijos no pueden eliminarse ni cambiar empresa/código/tipo. Se prepara además protección para cuentas con movimientos futuros. Los Grupos no admiten saldos de movimiento.
- **Buscar** filtra por código/nombre y grupo. La tabla muestra la jerarquía, clasificación Excel y saldos con dos decimales. No se registran asientos iniciales.
- Equivalencias: AC/ANC → Activo; PC/PNC → Pasivo; PAT → Patrimonio; ING → Ingresos; GAD/GVE/GFI/COS → Gastos; TRA → Transitorias. Una clasificación incompatible se rechaza.

## Fuentes pendientes

No están adjuntos el PDF ni el Excel. Se cargaron únicamente los cinco saldos de ejemplo indicados en la solicitud, sus grupos superiores y las seis raíces contables. **No es el catálogo completo ni una importación verificada de los documentos.** No se inventaron fechas para las tarifas ni se reemplazaron datos históricos de fuentes. Cuando estén disponibles se deben cotejar códigos, jerarquías, saldos y períodos antes de ampliar la carga inicial.

## Verificación automatizada

Desde PowerShell en la raíz del espacio de trabajo:

```powershell
.\Pruebas\Verificar.ps1
```

El script compila contra las referencias de .NET Framework 4.7.2, ejecuta las pruebas de servicios, compila todos los archivos incluidos en el `.csproj` y precompila las páginas/master con `aspnet_compiler`. Ajustar la ruta a Roslyn si Visual Studio está instalado en otro lugar. No ejecutar la compilación durante una sesión con datos que se deseen conservar: reinicia la aplicación.

Para pruebas HTTP, iniciar una instancia **recién reiniciada y sin usuarios**, ejecutarla localmente con IIS Express y luego:

```powershell
python Pruebas\pruebas_http.py http://localhost:5087
```

Estas pruebas crean usuarios, empresas y cuentas ficticios en memoria. Detener/reiniciar IIS Express después para descartarlos.

En este equipo MSBuild de Visual Studio no logra iniciar por una incompatibilidad de `System.Memory` (se reproduce también con su ejecutable de 64 bits). Por eso se incluye la verificación directa con Roslyn y `aspnet_compiler`; el problema de instalación de MSBuild requiere reparar Visual Studio si también aparece al compilar desde el IDE. No se modificaron los ensamblados de Visual Studio ni las dependencias existentes para ocultar el problema.

Resultado de la verificación realizada: compilación C# contra .NET Framework 4.7.2 y precompilación de las páginas correctas, **42 comprobaciones de servicios** y **21 comprobaciones HTTP** correctas con IIS Express. Incluyen creación inicial del administrador, login, registro público, lectura de todas las páginas, bloqueo de escritura para Usuario, inserción de empresa/tarifa/cuenta, rechazo de superposición, selección de usuario, protección del último administrador y cierre de sesión. No se realizó inspección visual en navegador; las páginas sí se renderizaron mediante solicitudes HTTP reales. La instancia de pruebas se detuvo al finalizar para descartar sus datos ficticios.
