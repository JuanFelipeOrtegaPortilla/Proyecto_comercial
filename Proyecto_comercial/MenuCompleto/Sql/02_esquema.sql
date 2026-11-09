-- Ejecutar UNA vez sobre la base vacía bd_sistema_contable. No elimina tablas existentes.
BEGIN;
CREATE EXTENSION IF NOT EXISTS btree_gist;
CREATE TABLE roles (
 id_rol serial PRIMARY KEY, nombre_rol varchar(30) NOT NULL UNIQUE,
 descripcion text NOT NULL DEFAULT '', CHECK(nombre_rol IN ('Usuario','Administrador'))
);
INSERT INTO roles(nombre_rol,descripcion) VALUES('Usuario','Consulta'),('Administrador','Mantenimiento');
CREATE TABLE usuarios (
 id_usuario serial PRIMARY KEY, nombres varchar(250) NOT NULL CHECK(btrim(nombres)<>''),
 apellidos varchar(250) NOT NULL CHECK(btrim(apellidos)<>''), nombre_usuario varchar(250) NOT NULL CHECK(btrim(nombre_usuario)<>''),
 correo varchar(250) NOT NULL, password_hash text NOT NULL CHECK(password_hash ~ '^[A-Za-z0-9+/=]+:[A-Za-z0-9+/=]+$'),
 estado boolean NOT NULL DEFAULT true, fecha_registro timestamptz NOT NULL DEFAULT current_timestamp
);
CREATE UNIQUE INDEX usuarios_nombre_unico ON usuarios(lower(btrim(nombre_usuario)));
CREATE TABLE usuario_roles (
 id_usuario integer PRIMARY KEY REFERENCES usuarios ON DELETE RESTRICT,
 id_rol integer NOT NULL REFERENCES roles ON DELETE RESTRICT
);
CREATE TABLE empresas (
 id_empresa serial PRIMARY KEY, ruc varchar(13) UNIQUE CHECK(ruc IS NULL OR ruc ~ '^[0-9]{13}$'),
 razon_social varchar(250) NOT NULL CHECK(btrim(razon_social)<>''), nombre_comercial varchar(250) DEFAULT '',
 direccion varchar(250) DEFAULT '', telefono varchar(250) DEFAULT '', correo varchar(250) DEFAULT '',
 representante_legal varchar(250) DEFAULT '', fecha_registro date NOT NULL, estado boolean NOT NULL DEFAULT true
);
CREATE TABLE plan_cuentas (
 id_cuenta serial PRIMARY KEY, id_empresa integer NOT NULL REFERENCES empresas ON DELETE RESTRICT,
 codigo_cuenta varchar(100) NOT NULL CHECK(codigo_cuenta ~ '^[1-6](\.[1-9][0-9]*)*$'),
 nombre_cuenta varchar(250) NOT NULL CHECK(btrim(nombre_cuenta)<>''),
 id_cuenta_padre integer REFERENCES plan_cuentas ON DELETE RESTRICT,
 codigo_cuenta_padre varchar(100), grupo_contable integer NOT NULL CHECK(grupo_contable BETWEEN 1 AND 6),
 nivel integer GENERATED ALWAYS AS (array_length(string_to_array(codigo_cuenta,'.'),1)) STORED,
 grupo_excel varchar(5) NOT NULL DEFAULT '', naturaleza varchar(15) NOT NULL CHECK(naturaleza IN ('Deudora','Acreedora')),
 tipo_cuenta varchar(15) NOT NULL CHECK(tipo_cuenta IN ('Grupo','Movimiento')),
 debe_inicial numeric(18,2) NOT NULL DEFAULT 0 CHECK(debe_inicial>=0),
 haber_inicial numeric(18,2) NOT NULL DEFAULT 0 CHECK(haber_inicial>=0),
 estado boolean NOT NULL DEFAULT true, tiene_movimientos boolean NOT NULL DEFAULT false,
 UNIQUE(id_empresa,codigo_cuenta), UNIQUE(id_empresa,id_cuenta),
 FOREIGN KEY(id_empresa,codigo_cuenta_padre) REFERENCES plan_cuentas(id_empresa,codigo_cuenta) ON DELETE RESTRICT,
 FOREIGN KEY(id_empresa,id_cuenta_padre) REFERENCES plan_cuentas(id_empresa,id_cuenta) ON DELETE RESTRICT,
 CHECK(grupo_contable=left(codigo_cuenta,1)::integer),
 CHECK(tipo_cuenta<>'Grupo' OR (debe_inicial=0 AND haber_inicial=0)),
 CHECK(grupo_excel='' OR (grupo_contable=1 AND grupo_excel IN ('AC','ANC')) OR
 (grupo_contable=2 AND grupo_excel IN ('PC','PNC')) OR (grupo_contable=3 AND grupo_excel='PAT') OR
 (grupo_contable=4 AND grupo_excel='ING') OR (grupo_contable=5 AND grupo_excel IN ('GAD','GVE','GFI','COS')) OR (grupo_contable=6 AND grupo_excel='TRA'))
);
CREATE TABLE tarifas_iva (
 id_tarifa serial PRIMARY KEY, id_empresa integer NOT NULL REFERENCES empresas ON DELETE RESTRICT,
 porcentaje numeric NOT NULL CHECK(porcentaje BETWEEN 0 AND 100), fecha_inicio date NOT NULL,
 fecha_fin date CHECK(fecha_fin IS NULL OR fecha_fin>=fecha_inicio), descripcion text NOT NULL DEFAULT '',
 utilizada boolean NOT NULL DEFAULT false, ultima_fecha_uso date,
 CHECK(utilizada=(ultima_fecha_uso IS NOT NULL)),
 CHECK(ultima_fecha_uso IS NULL OR (ultima_fecha_uso>=fecha_inicio AND (fecha_fin IS NULL OR ultima_fecha_uso<=fecha_fin))),
 EXCLUDE USING gist (id_empresa WITH =, daterange(fecha_inicio,fecha_fin,'[]') WITH &&)
);
CREATE FUNCTION bloquear_mantenimiento() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN PERFORM pg_advisory_xact_lock(72410301); RETURN NULL; END $$;
CREATE TRIGGER serializar_usuarios BEFORE INSERT OR UPDATE OR DELETE ON usuarios FOR EACH STATEMENT EXECUTE FUNCTION bloquear_mantenimiento();
CREATE TRIGGER serializar_roles BEFORE INSERT OR UPDATE OR DELETE ON usuario_roles FOR EACH STATEMENT EXECUTE FUNCTION bloquear_mantenimiento();
CREATE TRIGGER serializar_empresas BEFORE INSERT OR UPDATE OR DELETE ON empresas FOR EACH STATEMENT EXECUTE FUNCTION bloquear_mantenimiento();
CREATE TRIGGER serializar_cuentas BEFORE INSERT OR UPDATE OR DELETE ON plan_cuentas FOR EACH STATEMENT EXECUTE FUNCTION bloquear_mantenimiento();
CREATE TRIGGER serializar_tarifas BEFORE INSERT OR UPDATE OR DELETE ON tarifas_iva FOR EACH STATEMENT EXECUTE FUNCTION bloquear_mantenimiento();
CREATE FUNCTION validar_cuenta() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE padre text; encontrado plan_cuentas;
BEGIN
 IF TG_OP='DELETE' THEN
   IF OLD.tiene_movimientos THEN RAISE EXCEPTION 'Cuenta utilizada' USING ERRCODE='23514'; END IF;
   RETURN OLD;
 END IF;
 IF TG_OP='UPDATE' THEN
   IF OLD.tiene_movimientos AND (NOT NEW.tiene_movimientos OR (NEW.id_empresa,NEW.codigo_cuenta,NEW.tipo_cuenta,NEW.debe_inicial,NEW.haber_inicial) IS DISTINCT FROM (OLD.id_empresa,OLD.codigo_cuenta,OLD.tipo_cuenta,OLD.debe_inicial,OLD.haber_inicial)) THEN
     RAISE EXCEPTION 'Cuenta utilizada' USING ERRCODE='23514'; END IF;
   IF EXISTS(SELECT 1 FROM plan_cuentas WHERE id_cuenta_padre=OLD.id_cuenta) AND
      (NEW.tipo_cuenta<>'Grupo' OR NEW.id_empresa<>OLD.id_empresa OR NEW.codigo_cuenta<>OLD.codigo_cuenta) THEN
     RAISE EXCEPTION 'Cuenta con subcuentas' USING ERRCODE='23514'; END IF;
 END IF;
 padre=CASE WHEN position('.' IN NEW.codigo_cuenta)>0 THEN regexp_replace(NEW.codigo_cuenta,'\.[^.]+$','') ELSE NULL END;
 IF NEW.codigo_cuenta_padre IS DISTINCT FROM padre THEN RAISE EXCEPTION 'Padre inmediato inválido' USING ERRCODE='23514'; END IF;
 IF padre IS NOT NULL THEN
   SELECT * INTO encontrado FROM plan_cuentas WHERE id_empresa=NEW.id_empresa AND codigo_cuenta=padre;
   IF NOT FOUND OR encontrado.tipo_cuenta<>'Grupo' THEN RAISE EXCEPTION 'Padre inexistente o no agrupador' USING ERRCODE='23514'; END IF;
   NEW.id_cuenta_padre=encontrado.id_cuenta;
 ELSE NEW.id_cuenta_padre=NULL;
 END IF;
 RETURN NEW;
END $$;
CREATE TRIGGER integridad_cuenta BEFORE INSERT OR UPDATE OR DELETE ON plan_cuentas FOR EACH ROW EXECUTE FUNCTION validar_cuenta();
CREATE FUNCTION proteger_tarifa() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF OLD.utilizada THEN
   IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Historial protegido' USING ERRCODE='23514'; END IF;
   IF NOT NEW.utilizada OR (NEW.id_empresa,NEW.porcentaje,NEW.fecha_inicio) IS DISTINCT FROM (OLD.id_empresa,OLD.porcentaje,OLD.fecha_inicio)
      OR NEW.ultima_fecha_uso IS NULL OR NEW.ultima_fecha_uso<OLD.ultima_fecha_uso THEN
     RAISE EXCEPTION 'Historial protegido' USING ERRCODE='23514'; END IF;
 END IF;
 IF TG_OP='DELETE' THEN RETURN OLD; ELSE RETURN NEW; END IF;
END $$;
CREATE TRIGGER integridad_tarifa BEFORE UPDATE OR DELETE ON tarifas_iva FOR EACH ROW EXECUTE FUNCTION proteger_tarifa();
CREATE FUNCTION proteger_administrador() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF TG_OP='DELETE' AND NOT EXISTS(SELECT 1 FROM usuarios) THEN
   RAISE EXCEPTION 'No se puede eliminar el último administrador' USING ERRCODE='23514'; END IF;
 IF EXISTS(SELECT 1 FROM usuarios) AND NOT EXISTS(
 SELECT 1 FROM usuarios u JOIN usuario_roles ur USING(id_usuario) JOIN roles r USING(id_rol) WHERE u.estado AND r.nombre_rol='Administrador') THEN
   RAISE EXCEPTION 'Debe conservar un administrador activo' USING ERRCODE='23514';
 END IF;
 IF EXISTS(SELECT 1 FROM usuarios u WHERE NOT EXISTS(SELECT 1 FROM usuario_roles ur WHERE ur.id_usuario=u.id_usuario)) THEN
   RAISE EXCEPTION 'Usuario sin rol' USING ERRCODE='23514'; END IF;
 RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER administrador_usuario AFTER INSERT OR UPDATE OR DELETE ON usuarios DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION proteger_administrador();
CREATE CONSTRAINT TRIGGER administrador_rol AFTER INSERT OR UPDATE OR DELETE ON usuario_roles DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION proteger_administrador();
CREATE INDEX cuentas_padre ON plan_cuentas(id_cuenta_padre);
CREATE INDEX tarifas_empresa ON tarifas_iva(id_empresa,fecha_inicio);
COMMIT;
