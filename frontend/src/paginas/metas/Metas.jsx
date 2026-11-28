import { useEffect, useState } from 'react';
import { Plus, Target } from 'lucide-react';
import { useSearchParams } from 'react-router';
import { ESTADOS_META } from '../../api/metas';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Cargando from '../../componentes/Cargando';
import Confirmar from '../../componentes/Confirmar';
import EstadoError from '../../componentes/EstadoError';
import EstadoVacio from '../../componentes/EstadoVacio';
import PanelLateral from '../../componentes/PanelLateral';
import { useHabitos } from '../habitos/consultas';
import { useEliminarMeta, useMetas } from './consultas';
import FormularioMeta from './FormularioMeta';
import ReactivarMeta from './ReactivarMeta';
import TarjetaMeta from './TarjetaMeta';

const porFechaLimite = (a, b) => a.fechaLimite.localeCompare(b.fechaLimite);

// Metas agrupadas por estado: en curso (En progreso y Pendientes) arriba, luego las fallidas y al
// final las completadas, plegadas.
export default function Metas() {
  const metas = useMetas();
  const habitos = useHabitos();
  const [parametros, setParametros] = useSearchParams();
  const [aReactivar, setAReactivar] = useState(null);
  const [creada, setCreada] = useState(null);
  const [completada, setCompletada] = useState(null);
  const [aEliminar, setAEliminar] = useState(null);
  const [eliminada, setEliminada] = useState(null);
  const eliminar = useEliminarMeta();
  const creando = parametros.get('nueva') === '1';

  useEffect(() => {
    if (!creada && !completada && !eliminada) return undefined;
    const temporizador = setTimeout(() => {
      setCreada(null);
      setCompletada(null);
      setEliminada(null);
    }, 6000);
    return () => clearTimeout(temporizador);
  }, [creada, completada, eliminada]);

  const abrirCrear = () => setParametros({ nueva: '1' }, { replace: true });
  const cerrarCrear = () => setParametros({}, { replace: true });
  const nombres = new Map((habitos.data ?? []).map((h) => [h.id, h.nombre]));
  const nombresDe = (meta) => meta.habitoIds.map((id) => nombres.get(id)).filter(Boolean);

  let contenido;
  if (metas.isPending) contenido = <Cargando texto="Cargando tus metas…" />;
  else if (metas.isError) contenido = <EstadoError error={metas.error} alReintentar={metas.refetch} />;
  else if (metas.data.length === 0) {
    contenido = (
      <EstadoVacio
        icono={Target}
        titulo="Aún no tienes metas"
        accion={
          <Boton onClick={abrirCrear}>
            <Plus className="size-4" aria-hidden="true" />
            Crear tu primera meta
          </Boton>
        }
      >
        Una meta agrupa hábitos con un objetivo, por ejemplo «Leer 20 veces antes de fin de mes».
      </EstadoVacio>
    );
  } else {
    const enCurso = metas.data
      .filter((m) => m.estado === ESTADOS_META.EnProgreso || m.estado === ESTADOS_META.Pendiente)
      .sort((a, b) => (a.estado === b.estado ? porFechaLimite(a, b) : a.estado === ESTADOS_META.EnProgreso ? -1 : 1));
    const fallidas = metas.data.filter((m) => m.estado === ESTADOS_META.Fallida).sort(porFechaLimite);
    const completadas = metas.data.filter((m) => m.estado === ESTADOS_META.Completada).sort((a, b) => porFechaLimite(b, a));
    const tarjeta = (meta) => <TarjetaMeta key={meta.id} meta={meta} nombresHabitos={nombresDe(meta)} alReactivar={setAReactivar} alCompletar={setCompletada} alEliminar={setAEliminar} />;

    contenido = (
      <div className="flex flex-col gap-8">
        <Grupo titulo="En curso" cantidad={enCurso.length}>
          {enCurso.length ? enCurso.map(tarjeta) : <p className="text-sm text-suave">No tienes metas en curso.</p>}
        </Grupo>
        {fallidas.length > 0 && (
          <Grupo titulo="Fallidas" cantidad={fallidas.length}>
            {fallidas.map(tarjeta)}
          </Grupo>
        )}
        {completadas.length > 0 && (
          <details className="group flex flex-col gap-3">
            <summary className="cursor-pointer list-none text-sm font-semibold tracking-wide text-suave uppercase select-none">
              <span className="inline-block transition-transform group-open:rotate-90" aria-hidden="true">
                ▸
              </span>{' '}
              Completadas <span className="numeros">({completadas.length})</span>
            </summary>
            <div className="mt-3 grid gap-3 lg:grid-cols-2">{completadas.map(tarjeta)}</div>
          </details>
        )}
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-2xl font-semibold">Tus metas</h2>
        {metas.data?.length > 0 && (
          <Boton onClick={abrirCrear}>
            <Plus className="size-4" aria-hidden="true" />
            Nueva meta
          </Boton>
        )}
      </div>
      {completada && <Aviso tipo="exito">¡Completaste «{completada}»!</Aviso>}
      {creada && <Aviso tipo="exito">Creaste la meta «{creada}».</Aviso>}
      {eliminada && <Aviso tipo="exito">Eliminaste la meta «{eliminada}».</Aviso>}
      {contenido}

      <PanelLateral abierto={creando} alCerrar={cerrarCrear} titulo="Nueva meta">
        {habitos.isPending ? (
          <Cargando />
        ) : habitos.isError ? (
          <EstadoError error={habitos.error} alReintentar={habitos.refetch} />
        ) : (
          <FormularioMeta
            habitos={habitos.data}
            alCrear={(meta) => {
              setCreada(meta.nombre);
              cerrarCrear();
            }}
          />
        )}
      </PanelLateral>
      <ReactivarMeta meta={aReactivar} alCerrar={() => setAReactivar(null)} />
      <Confirmar
        abierto={Boolean(aEliminar)}
        alCerrar={() => setAEliminar(null)}
        titulo={`¿Eliminar la meta «${aEliminar?.nombre ?? ''}»?`}
        descripcion="La meta deja de existir; sus hábitos y sus registros no cambian. No se puede deshacer."
        textoBoton="Eliminar meta"
        peligroso
        accion={eliminar}
        alConfirmar={() =>
          eliminar.mutate(aEliminar.id, {
            onSuccess: () => {
              setEliminada(aEliminar.nombre);
              setAEliminar(null);
            },
          })
        }
      />
    </div>
  );
}

function Grupo({ titulo, cantidad, children }) {
  return (
    <section aria-label={titulo} className="flex flex-col gap-3">
      <h3 className="text-sm font-semibold tracking-wide text-suave uppercase">
        {titulo} <span className="numeros">({cantidad})</span>
      </h3>
      <div className="grid gap-3 lg:grid-cols-2">{children}</div>
    </section>
  );
}
