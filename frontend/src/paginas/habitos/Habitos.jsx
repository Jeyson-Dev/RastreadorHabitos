import { useEffect, useState } from 'react';
import { ChevronRight, ListChecks, Plus } from 'lucide-react';
import { useSearchParams } from 'react-router';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Cargando from '../../componentes/Cargando';
import EstadoError from '../../componentes/EstadoError';
import EstadoVacio from '../../componentes/EstadoVacio';
import PanelLateral from '../../componentes/PanelLateral';
import { describirDias } from '../../utilidades/fechas';
import { PERMISO_LIMITE_AMPLIADO } from '../../api/solicitudes';
import { useSesion } from '../../sesion/contextoSesion';
import AvisoLimite from './AvisoLimite';
import { useHabitos } from './consultas';
import DetalleHabito from './DetalleHabito';
import FormularioHabito from './FormularioHabito';
import Racha from './Racha';
import TiraDias, { LeyendaDias } from './TiraDias';

// Mis hábitos: cada uno con sus días, rachas y los últimos 30 días; al tocarlo se abre su detalle.
// «?nuevo=1» abre directamente el panel para crear (lo usa la pantalla Hoy).
export default function Habitos() {
  const habitos = useHabitos();
  const { tienePermiso } = useSesion();
  const ampliado = tienePermiso(PERMISO_LIMITE_AMPLIADO);
  const limite = ampliado ? 20 : 10;
  const [parametros, setParametros] = useSearchParams();
  const [idDetalle, setIdDetalle] = useState(null);
  const [creado, setCreado] = useState(null);
  const creando = parametros.get('nuevo') === '1';

  useEffect(() => {
    if (!creado) return undefined;
    const temporizador = setTimeout(() => setCreado(null), 5000);
    return () => clearTimeout(temporizador);
  }, [creado]);

  const abrirCrear = () => setParametros({ nuevo: '1' }, { replace: true });
  const cerrarCrear = () => setParametros({}, { replace: true });
  const detalle = habitos.data?.find((h) => h.id === idDetalle);

  let contenido;
  if (habitos.isPending) contenido = <Cargando texto="Cargando tus hábitos…" />;
  else if (habitos.isError) contenido = <EstadoError error={habitos.error} alReintentar={habitos.refetch} />;
  else if (habitos.data.length === 0) {
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
        <ul className="flex flex-col divide-y divide-borde overflow-hidden rounded-xl border border-borde bg-superficie">
          {habitos.data.map((habito) => (
            <li key={habito.id}>
              <button
                type="button"
                onClick={() => setIdDetalle(habito.id)}
                aria-describedby={`tira-${habito.id}`}
                className="flex w-full flex-col gap-3 px-4 py-4 text-left hover:bg-superficie-suave sm:px-5"
              >
                <span className="flex items-start justify-between gap-3">
                  <span className="flex min-w-0 flex-col">
                    <span className="truncate font-medium">{habito.nombre}</span>
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
            </li>
          ))}
        </ul>
        <LeyendaDias />
      </>
    );
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="font-semibold">
          Tus hábitos{' '}
          {habitos.data && (
            <span className="numeros ml-1 text-sm font-normal text-suave">
              {habitos.data.length} de {limite}
            </span>
          )}
        </h2>
        {habitos.data?.length > 0 && (
          <Boton onClick={abrirCrear}>
            <Plus className="size-4" aria-hidden="true" />
            Nuevo hábito
          </Boton>
        )}
      </div>
      {creado && <Aviso tipo="exito">Creaste «{creado}».</Aviso>}
      {habitos.data && <AvisoLimite cantidad={habitos.data.length} limite={limite} ampliado={ampliado} />}
      {contenido}

      <PanelLateral abierto={creando} alCerrar={cerrarCrear} titulo="Nuevo hábito">
        <FormularioHabito
          alCrear={(habito) => {
            setCreado(habito.nombre);
            cerrarCrear();
          }}
        />
      </PanelLateral>
      <PanelLateral abierto={Boolean(detalle)} alCerrar={() => setIdDetalle(null)} titulo={detalle?.nombre ?? ''}>
        {detalle && <DetalleHabito habito={detalle} />}
      </PanelLateral>
    </div>
  );
}
