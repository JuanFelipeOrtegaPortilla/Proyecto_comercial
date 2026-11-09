-- Migración ADITIVA: ejecutar en bd_sistema_contable desde pgAdmin, sin repetir 01/02/03.
-- No elimina ni reinicializa registros existentes.
BEGIN;
SELECT pg_advisory_xact_lock(72410301);
CREATE TABLE IF NOT EXISTS transacciones (
 id_transaccion serial PRIMARY KEY,
 id_empresa integer NOT NULL REFERENCES empresas ON DELETE RESTRICT,
 fecha date NOT NULL, glosa varchar(250) NOT NULL CHECK(btrim(glosa)<>''),
 tipo varchar(15) NOT NULL CHECK(tipo IN ('Manual','Compra','Venta')),
 estado varchar(15) NOT NULL CHECK(estado IN ('Borrador','Confirmada')),
 tratamiento varchar(15) NOT NULL CHECK(tratamiento IN ('Gravado','TarifaCero','NoObjeto','Exento')),
 subtotal numeric(18,2) NOT NULL DEFAULT 0 CHECK(subtotal>=0),
 id_tarifa integer REFERENCES tarifas_iva ON DELETE RESTRICT,
 porcentaje_aplicado numeric NOT NULL DEFAULT 0 CHECK(porcentaje_aplicado BETWEEN 0 AND 100),
 iva numeric(18,2) NOT NULL DEFAULT 0 CHECK(iva>=0), total numeric(18,2) NOT NULL DEFAULT 0,
 cuenta_base integer REFERENCES plan_cuentas ON DELETE RESTRICT,
 cuenta_iva integer REFERENCES plan_cuentas ON DELETE RESTRICT,
 contrapartida integer REFERENCES plan_cuentas ON DELETE RESTRICT,
 CHECK(total=subtotal+iva), CHECK(iva=round(subtotal*porcentaje_aplicado/100,2)),
 CHECK((tipo='Manual' AND subtotal=0 AND id_tarifa IS NULL AND cuenta_base IS NULL AND contrapartida IS NULL) OR
       (tipo IN ('Compra','Venta') AND subtotal>0 AND cuenta_base IS NOT NULL AND contrapartida IS NOT NULL)),
 CHECK((tratamiento IN ('Exento','NoObjeto') AND id_tarifa IS NULL AND porcentaje_aplicado=0) OR
       (tratamiento IN ('Gravado','TarifaCero') AND id_tarifa IS NOT NULL)),
 CHECK(tratamiento<>'TarifaCero' OR porcentaje_aplicado=0)
);
CREATE TABLE IF NOT EXISTS lineas_asiento (
 id_linea serial PRIMARY KEY, id_transaccion integer NOT NULL REFERENCES transacciones ON DELETE RESTRICT,
 id_cuenta integer NOT NULL REFERENCES plan_cuentas ON DELETE RESTRICT,
 debe numeric(18,2) NOT NULL DEFAULT 0 CHECK(debe>=0), haber numeric(18,2) NOT NULL DEFAULT 0 CHECK(haber>=0),
 CHECK((debe>0 AND haber=0) OR (haber>0 AND debe=0))
);
CREATE INDEX IF NOT EXISTS transacciones_periodo ON transacciones(id_empresa,fecha,estado);
CREATE INDEX IF NOT EXISTS lineas_cuenta ON lineas_asiento(id_cuenta,id_transaccion);
CREATE OR REPLACE FUNCTION proteger_transaccion() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF TG_OP='DELETE' THEN
   IF OLD.estado='Confirmada' THEN RAISE EXCEPTION 'Asiento confirmado inmutable' USING ERRCODE='23514'; END IF;
   RETURN OLD;
 END IF;
 IF TG_OP='UPDATE' AND OLD.estado='Confirmada' AND NEW IS DISTINCT FROM OLD THEN
   RAISE EXCEPTION 'Asiento confirmado inmutable' USING ERRCODE='23514'; END IF;
 IF NEW.estado='Confirmada' AND NEW.id_tarifa IS NOT NULL AND NOT EXISTS(
   SELECT 1 FROM tarifas_iva t WHERE t.id_tarifa=NEW.id_tarifa AND t.id_empresa=NEW.id_empresa
   AND t.porcentaje=NEW.porcentaje_aplicado AND NEW.fecha>=t.fecha_inicio AND (t.fecha_fin IS NULL OR NEW.fecha<=t.fecha_fin)
   AND t.utilizada AND t.ultima_fecha_uso>=NEW.fecha) THEN
   RAISE EXCEPTION 'Tarifa no confirmada o fuera de vigencia' USING ERRCODE='23514'; END IF;
 RETURN NEW;
END $$;
CREATE OR REPLACE FUNCTION proteger_linea() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE cabecera transacciones; cuenta plan_cuentas;
BEGIN
 IF TG_OP IN ('UPDATE','DELETE') AND EXISTS(SELECT 1 FROM transacciones WHERE id_transaccion=OLD.id_transaccion AND estado='Confirmada') THEN
   RAISE EXCEPTION 'Asiento confirmado inmutable' USING ERRCODE='23514'; END IF;
 IF TG_OP='DELETE' THEN RETURN OLD; END IF;
 SELECT * INTO cabecera FROM transacciones WHERE id_transaccion=NEW.id_transaccion;
 SELECT * INTO cuenta FROM plan_cuentas WHERE id_cuenta=NEW.id_cuenta;
 IF cabecera.estado='Confirmada' OR cabecera.id_empresa IS DISTINCT FROM cuenta.id_empresa OR cuenta.tipo_cuenta<>'Movimiento' OR NOT cuenta.estado THEN
   RAISE EXCEPTION 'Cuenta o asiento inválido' USING ERRCODE='23514'; END IF;
 RETURN NEW;
END $$;
CREATE OR REPLACE FUNCTION cuadrar_asiento() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE referencia integer; estado_actual text; diferencia numeric; cantidad integer;
BEGIN
 IF TG_TABLE_NAME='transacciones' THEN referencia=COALESCE(NEW.id_transaccion,OLD.id_transaccion);
 ELSE referencia=COALESCE(NEW.id_transaccion,OLD.id_transaccion); END IF;
 SELECT estado INTO estado_actual FROM transacciones WHERE id_transaccion=referencia;
 IF estado_actual='Confirmada' THEN
   SELECT count(*),sum(debe-haber) INTO cantidad,diferencia FROM lineas_asiento WHERE id_transaccion=referencia;
   IF cantidad<2 OR diferencia<>0 THEN RAISE EXCEPTION 'Asiento descuadrado' USING ERRCODE='23514'; END IF;
 END IF;
 RETURN NULL;
END $$;
DROP TRIGGER IF EXISTS serializar_transacciones ON transacciones;
CREATE TRIGGER serializar_transacciones BEFORE INSERT OR UPDATE OR DELETE ON transacciones FOR EACH STATEMENT EXECUTE FUNCTION bloquear_mantenimiento();
DROP TRIGGER IF EXISTS serializar_lineas ON lineas_asiento;
CREATE TRIGGER serializar_lineas BEFORE INSERT OR UPDATE OR DELETE ON lineas_asiento FOR EACH STATEMENT EXECUTE FUNCTION bloquear_mantenimiento();
DROP TRIGGER IF EXISTS integridad_transaccion ON transacciones;
CREATE TRIGGER integridad_transaccion BEFORE INSERT OR UPDATE OR DELETE ON transacciones FOR EACH ROW EXECUTE FUNCTION proteger_transaccion();
DROP TRIGGER IF EXISTS integridad_linea ON lineas_asiento;
CREATE TRIGGER integridad_linea BEFORE INSERT OR UPDATE OR DELETE ON lineas_asiento FOR EACH ROW EXECUTE FUNCTION proteger_linea();
DROP TRIGGER IF EXISTS cuadratura_cabecera ON transacciones;
CREATE CONSTRAINT TRIGGER cuadratura_cabecera AFTER INSERT OR UPDATE ON transacciones DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cuadrar_asiento();
DROP TRIGGER IF EXISTS cuadratura_linea ON lineas_asiento;
CREATE CONSTRAINT TRIGGER cuadratura_linea AFTER INSERT OR UPDATE OR DELETE ON lineas_asiento DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION cuadrar_asiento();
COMMIT;
