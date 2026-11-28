import { useEffect, useState } from 'react';
import { Archive, ArchiveRestore, ChevronRight, ListChecks, Pencil, Plus, Trash2 } from 'lucide-react';
import { m } from 'motion/react';
import { useSearchParams } from 'react-router';
import { mensajeDeError } from '../../api/cliente';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Cargando from '../../componentes/Cargando';
import Confirmar from '../../componentes/Confirmar';
import EstadoError from '../../componentes/EstadoError';
import EstadoVacio from '../../componentes/EstadoVacio';
import IconoHabito from '../../componentes/IconoHabito';
import MenuAcciones from '../../componentes/MenuAcciones';
import PanelLateral from '../../componentes/PanelLateral';
import { describirDias, diaLocalDe, fechaCorta, instanteUtc } from '../../utilidades/fechas';
import { useEscalonado } from '../../utilidades/movimiento';
import { PERMISO_LIMITE_AMPLIADO } from '../../api/solicitudes';
import { useSesion } from '../../sesion/contextoSesion';
import { useMetas } from '../metas/consultas';
import AvisoLimite from './AvisoLimite';
import { useArchivarHabito, useEliminarHabito, useHabitos, useRestaurarHabito } from './consultas';
import DetalleHabito from './DetalleHabito';
import FormularioHabito from './FormularioHabito';
import Racha from './Racha';
import TiraDias, { LeyendaDias } from './TiraDias';

function fechaDeArchivo(habito) {
  const instante = instanteUtc(habito.fechaArchivadoUtc);
  return instante ? fechaCorta(diaLocalDe(instante)) : '';
}

// Mis hábitos: los activos con sus días, rachas y los últimos 30 días; al tocar uno se abre su
// detalle, y el menú «⋯» lo edita, archiva o elimina. Los archivados van al final, plegados.
// «?nuevo=1» abre directamente el panel para crear (lo usa la pantalla Hoy).
export default function Habitos() {
  const habitos = useHabitos();
  const archivar = useArchivarHabito();
  const restaurar = useRestaurarHabito();
  const eliminar = useEliminarHabito();
  const { tienePermiso } = useSesion();
  const ampliado = tienePermiso(PERMISO_LIMITE_AMPLIADO);
  const limite = ampliado ? 20 : 10;
  const [parametros, setParametros] = useSearchParams();
  const [idDetalle, setIdDetalle] = useState(null);
  const [idEditar, setIdEditar] = useState(null);
  const [aEliminar, setAEliminar] = useState(null);
  // Las metas solo hacen falta para avisar cuántas pierden el hábito al eliminarlo.
  const metas = useMetas({ enabled: Boolean(aEliminar) });
  const [mensaje, setMensaje] = useState(null);
  const [errorAccion, setErrorAccion] = useState(null);
  const { contenedor, elemento } = useEscalonado();
  const creando = parametros.get('nuevo') === '1';

  useEffect(() => {
    if (!mensaje) return undefined;
    const temporizador = setTimeout(() => setMensaje(null), 5000);
    return () => clearTimeout(temporizador);
  }, [mensaje]);

  const abrirCrear = () => setParametros({ nuevo: '1' }, { replace: true });
  const cerrarCrear = () => setParametros({}, { replace: true });
  const todos = habitos.data ?? [];
  const activos = todos.filter((h) => !h.archivado);
  const archivados = todos.filter((h) => h.archivado);
  const detalle = todos.find((h) => h.id === idDetalle);
  const enEdicion = todos.find((h) => h.id === idEditar);
  const metasAfectadas = aEliminar ? (metas.data ?? []).filter((m) => m.habitoIds.includes(aEliminar.id)).length : 0;

  // Archivar y restaurar se hacen al instante (se pueden deshacer); el resultado se anuncia arriba.
  function cambiarArchivo(habito, accion) {
    setErrorAccion(null);
    const mutacion = accion === 'archivar' ? archivar : restaurar;
    mutacion.mutate(habito.id, {
      onSuccess: () => setMensaje(accion === 'archivar' ? `Archivaste «${habito.nombre}». Su historial y su racha se conservan.` : `Restauraste «${habito.nombre}».`),
      onError: (error) => setErrorAccion(mensajeDeError(error)),
    });
  }

  const accionesDe = (habito) =>
    habito.archivado
      ? [
          { etiqueta: 'Restaurar', icono: ArchiveRestore, alElegir: () => cambiarArchivo(habito, 'restaurar') },
          { etiqueta: 'Eliminar', icono: Trash2, alElegir: () => setAEliminar(habito), peligroso: true },
        ]
      : [
          { etiqueta: 'Editar', icono: Pencil, alElegir: () => setIdEditar(habito.id) },
          { etiqueta: 'Archivar', icono: Archive, alElegir: () => cambiarArchivo(habito, 'archivar') },
          { etiqueta: 'Eliminar', icono: Trash2, alElegir: () => setAEliminar(habito), peligroso: true },
        ];

  let contenido;
  if (habitos.isPending) contenido = <Cargando texto="Cargando tus hábitos…" />;
  else if (habitos.isError) contenido = <EstadoError error={habitos.error} alReintentar={habitos.refetch} />;
  else if (todos.length === 0) {
    contenido = (
      <EstadoVacio
        icono={ListChecks}
        titulo="Aún no tienes hábitos"
        accion={
          <Boton onClick={abrirCrear}>
            <Plus className="size-4" aria-hidden="true" />
            Crear tu primer hábito
          </Boton>
        }
      >
        Elige algo pequeño que quieras repetir y los días de la semana en que lo harás.
      </EstadoVacio>
    );
  } else {
    contenido = (
      <>
        {activos.length === 0 ? (
          <p className="vidrio rounded-[1.25rem] p-5 text-sm text-suave">Todos tus hábitos están archivados. Restaura uno o crea uno nuevo.</p>
        ) : (
          <m.ul {...contenedor} className="flex flex-col gap-2.5">
            {activos.map((habito) => (
              <m.li key={habito.id} variants={elemento.variants} className="relative has-[[aria-expanded=true]]:z-20 vidrio flex items-start gap-1 rounded-[1.25rem] p-1.5 pr-2">
                <button
                  type="button"
                  onClick={() => setIdDetalle(habito.id)}
                  aria-describedby={`tira-${habito.id}`}
                  className="flex min-w-0 flex-1 flex-col gap-3 rounded-2xl px-3 py-3 text-left transition-colors hover:bg-superficie-suave/50 sm:px-4"
                >
                  <span className="flex items-center gap-3">
                    <IconoHabito habito={habito} />
                    <span className="flex min-w-0 flex-1 flex-col">
                      <span className="truncate font-semibold">{habito.nombre}</span>
                      <span className="text-xs text-suave">{describirDias(habito.dias)}</span>
                    </span>
                    <span className="flex shrink-0 items-center gap-3">
                      <Racha dias={habito.rachaActual} />
                      <span className="numeros hidden text-xs text-suave sm:inline">mejor {habito.mejorRacha}</span>
                      <ChevronRight className="size-4 text-suave" aria-hidden="true" />
                    </span>
                  </span>
                  <TiraDias habito={habito} idResumen={`tira-${habito.id}`} />
                </button>
                <div className="pt-2.5">
                  <MenuAcciones etiqueta={`Acciones de ${habito.nombre}`} acciones={accionesDe(habito)} />
                </div>
              </m.li>
            ))}
          </m.ul>
        )}
        <LeyendaDias />

        {archivados.length > 0 && (
          <details className="group flex flex-col gap-3">
            <summary className="flex cursor-pointer list-none items-center gap-2 text-sm font-semibold tracking-wide text-suave uppercase select-none">
              <ChevronRight className="size-4 transition-transform group-open:rotate-90" aria-hidden="true" />
              Archivados <span className="numeros">({archivados.length})</span>
            </summary>
            <ul className="mt-3 flex flex-col gap-2">
              {archivados.map((habito) => (
                <li key={habito.id} className="relative has-[[aria-expanded=true]]:z-20 vidrio flex items-center gap-3 rounded-2xl px-4 py-3">
                  <IconoHabito habito={habito} tamano="sm" />
                  <span className="flex min-w-0 flex-1 flex-col">
                    <span className="truncate font-medium">{habito.nombre}</span>
                    <span className="text-xs text-suave">
                      Archivado el {fechaDeArchivo(habito)} · racha guardada: {habito.rachaActual}
                    </span>
                  </span>
                  <MenuAcciones etiqueta={`Acciones de ${habito.nombre}`} acciones={accionesDe(habito)} />
                </li>
              ))}
            </ul>
          </details>
        )}
      </>
    );
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-2xl font-semibold">
          Tus hábitos{' '}
          {habitos.data && (
            <span className="numeros ml-1 font-sans text-sm font-normal text-suave">
              {activos.length} de {limite}
            </span>
          )}
        </h2>
        {todos.length > 0 && (
          <Boton onClick={abrirCrear}>
            <Plus className="size-4" aria-hidden="true" />
            Nuevo hábito
          </Boton>
        )}
      </div>
      {mensaje && <Aviso tipo="exito">{mensaje}</Aviso>}
      {errorAccion && <Aviso tipo="error">{errorAccion}</Aviso>}
      {habitos.data && <AvisoLimite cantidad={activos.length} limite={limite} ampliado={ampliado} />}
      {contenido}

      <PanelLateral abierto={creando} alCerrar={cerrarCrear} titulo="Nuevo hábito">
        <FormularioHabito
          alCrear={(habito) => {
            setMensaje(`Creaste «${habito.nombre}».`);
            cerrarCrear();
          }}
        />
      </PanelLateral>
      <PanelLateral abierto={Boolean(enEdicion)} alCerrar={() => setIdEditar(null)} titulo="Editar hábito">
        {enEdicion && (
          <FormularioHabito
            habito={enEdicion}
            alGuardar={(habito) => {
              setMensaje(`Guardaste los cambios de «${habito.nombre}».`);
              setIdEditar(null);
            }}
          />
        )}
      </PanelLateral>
      <PanelLateral abierto={Boolean(detalle)} alCerrar={() => setIdDetalle(null)} titulo={detalle?.nombre ?? ''}>
        {detalle && (
          <DetalleHabito
            habito={detalle}
            alEditar={() => {
              setIdDetalle(null);
              setIdEditar(detalle.id);
            }}
            alArchivar={() => {
              setIdDetalle(null);
              cambiarArchivo(detalle, 'archivar');
            }}
            alEliminar={() => {
              setIdDetalle(null);
              setAEliminar(detalle);
            }}
          />
        )}
      </PanelLateral>
      <Confirmar
        abierto={Boolean(aEliminar)}
        alCerrar={() => setAEliminar(null)}
        titulo={`¿Eliminar «${aEliminar?.nombre ?? ''}»?`}
        descripcion={`Se borran el hábito y todo su historial; no se puede deshacer.${
          metasAfectadas > 0 ? ` ${metasAfectadas === 1 ? 'Una meta deja' : `${metasAfectadas} metas dejan`} de contarlo.` : ''
        } Si solo quieres pausarlo, archívalo.`}
        textoBoton="Eliminar hábito"
        peligroso
        accion={eliminar}
        alConfirmar={() =>
          eliminar.mutate(aEliminar.id, {
            onSuccess: () => {
              setMensaje(`Eliminaste «${aEliminar.nombre}».`);
              setAEliminar(null);
            },
          })
        }
      />
    </div>
  );
}
