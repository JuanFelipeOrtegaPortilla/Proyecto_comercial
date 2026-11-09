# Transacciones, Diario y Mayor

Se incorporaron al proyecto existente `Transacciones.aspx`, `LibroDiario.aspx` y `LibroMayor.aspx`, con servicios, modelos y acceso PostgreSQL. La migración aditiva `MenuCompleto/Sql/04_modulos_contables.sql` ya se ejecutó contra la conexión local configurada. No se repitieron los scripts iniciales ni se modificaron registros contables previos. Para otra instalación, ejecutar únicamente esta migración después del esquema inicial.

## Texto y codificación

El problema de la captura era texto UTF-8 interpretado con otra codificación. Se especificó UTF-8 en `globalization` para lectura de archivos, solicitudes y respuestas; se añadió BOM a las páginas, controles y Master Page, y `PaginaBase` establece la cabecera UTF-8. Se preservaron las tildes y los datos almacenados. Una consulta HTTP comprobó la cabecera y el texto exacto `Módulos del sistema`, sin caracteres corruptos. Recargar la página después de reiniciar la aplicación para descartar el documento anterior.

## Uso

1. Como administrador, abrir **Transacciones** y seleccionar empresa. Los usuarios básicos pueden consultar, pero no guardar, editar, eliminar ni confirmar.
2. Seleccionar **Compra** o **Venta**, ingresar fecha, glosa, subtotal con punto decimal y tratamiento tributario. Seleccionar las cuentas base, IVA si corresponde y contrapartida. La venta utiliza una cuenta de Ingresos como base; la compra utiliza Activo o Gastos. Todas las cuentas deben estar activas, ser de Movimiento y pertenecer a la empresa.
3. **Guardar borrador** calcula el IVA de la fecha configurada y genera las líneas. No se presupone ningún porcentaje; si falta tarifa para una operación gravada, se rechaza. Exento y NoObjeto son tratamientos explícitos sin tarifa; TarifaCero exige una tarifa de 0 %. Los importes se redondean según el servicio monetario existente.
4. Para un asiento **Manual**, agregar al menos dos líneas con un importe positivo en Debe o Haber, nunca ambos. No calcula IVA automáticamente: las cuentas y los importes son los definidos por el administrador para ese asiento. Se valida la igualdad Debe/Haber antes de guardar.
5. Seleccionar un borrador en la tabla permite cargarlo y actualizarlo o eliminarlo con confirmación. Si se editan los controles, pulsar **Actualizar borrador** antes de confirmar. **Nuevo** inicia otro registro; Guardar solo está habilitado para registros nuevos para reducir duplicados accidentales.
6. **Confirmar borrador guardado** registra el asiento en los libros y protege las cuentas y la tarifa utilizada. La confirmación conserva porcentaje, subtotal, IVA y total. Si la configuración cambió desde que se guardó el borrador, debe actualizarse antes de confirmar. La confirmación es atómica en PostgreSQL.
7. Los asientos confirmados son inmutables: no se editan ni eliminan. Para corregirlos, registrar un asiento manual compensatorio y después el asiento correcto, referenciando el ID original en la glosa. No se implementó una anulación automática ni reapertura de períodos.
8. **Libro Diario**: seleccionar empresa y rango de fechas inclusivo, pulsar Consultar. Solo muestra asientos confirmados; incluye código, nombre, glosa, Debe/Haber y totales del período.
9. **Libro Mayor**: seleccionar empresa, cuenta de Movimiento y período. Incluye saldo anterior, movimientos y saldo acumulado. El saldo anterior incorpora DebeInicial menos HaberInicial del plan y los movimientos confirmados anteriores al período. Un saldo positivo es deudor; uno negativo es acreedor. No se crea automáticamente un asiento de apertura, evitando duplicar los saldos iniciales.

El ID del asiento es un identificador interno; no es un número de factura autorizado ni una numeración fiscal. No se asigna un año real al período `200X` de las fuentes y no se importan automáticamente sus operaciones. La fecha debe ser elegida expresamente.

## Alcance de esta continuación

Compra y Venta generan el asiento monetario de adquisición o ingreso y su IVA. **Todavía no hay control de artículos ni Kárdex**, por lo que una venta no descuenta existencias ni calcula automáticamente su costo. Ese costo puede registrarse mediante asiento manual hasta implementar inventarios. Tampoco se implementaron retenciones, balance ajustado, estados financieros, cierre o postcierre; el menú conserva esos módulos como pendientes.

Los saldos iniciales permanecen protegidos cuando las cuentas tienen movimientos. No registrar esos mismos saldos nuevamente como un asiento de apertura si se utilizan las consultas actuales del Mayor.

## Archivos y verificación

- `Modelos/Transaccion.cs`: cabecera, líneas y filas de libros.
- `Servicios/TransaccionService.cs`: borradores, IVA, validaciones, confirmación, Diario y Mayor.
- `Datos/UnidadPostgres.cs`: mapeo relacional de las tablas nuevas dentro de la misma unidad de trabajo. Las páginas anteriores siguen disponibles si una instalación aún no ejecutó la migración; el módulo contable muestra la instrucción de instalación.
- `Infraestructura/LibroContable.cs`: controles y consultas compartidos de los libros.
- Nuevas páginas con sus archivos de código y designer; menú y catálogo actualizados.
- `Sql/04_modulos_contables.sql`: tablas, relaciones restrictivas y triggers de cuadratura/protección. La migración no vacía tablas y puede volver a instalar sus triggers.
- `Pruebas/PruebasServicios.cs`: 58 comprobaciones correctas en memoria, incluidas las nuevas reglas contables.
- `Pruebas/contabilidad_rollback.sql`: prueba real de PostgreSQL con creación de registros temporales, cuadratura y protección de historial; termina con ROLLBACK. Se ejecutó correctamente. Las secuencias pueden tener saltos normales después de este tipo de prueba.

Compilación C# y precompilación ASP.NET comprobadas con las referencias .NET Framework 4.7.2. La prueba de HTTP para UTF-8 no necesitó iniciar sesión ni alterar usuarios. No se realizó una prueba de registro de operaciones desde una sesión autenticada en el navegador sobre tus datos reales.
