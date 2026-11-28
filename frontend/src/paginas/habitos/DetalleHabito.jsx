import { Archive, Flame, Pencil, Trash2, Trophy } from 'lucide-react';
import Boton from '../../componentes/Boton';
import EstadoError from '../../componentes/EstadoError';
import IconoHabito from '../../componentes/IconoHabito';
import { describirDias } from '../../utilidades/fechas';
import BotonesRegistro from './BotonesRegistro';
import CalendarioMes from './CalendarioMes';
import { useRegistros } from './consultas';
import { LeyendaDias } from './TiraDias';

// Detalle de un hábito dentro del panel lateral: identidad, rachas, registro de hoy, calendario del
// mes y las acciones (editar, archivar y eliminar, esta última con confirmación en la página).
export default function DetalleHabito({ habito, alEditar, alArchivar, alEliminar }) {
  const historial = useRegistros(habito.id);
  const hoy = historial.data?.hasta;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-start gap-4">
        <IconoHabito habito={habito} tamano="lg" />
        <div className="flex min-w-0 flex-col gap-1">
          <p className="text-sm text-suave">{describirDias(habito.dias)}</p>
          {habito.descripcion && <p className="text-sm">{habito.descripcion}</p>}
        </div>
      </div>

      <dl className="grid grid-cols-2 gap-3">
        <div className="rounded-2xl bg-[linear-gradient(140deg,color-mix(in_oklab,var(--racha)_16%,transparent),transparent)] p-4 ring-1 ring-borde/70">
          <dt className="flex items-center gap-1.5 text-xs text-suave">
            <Flame className="size-4 text-racha" aria-hidden="true" />
            Racha actual
          </dt>
          <dd className="titulo numeros text-3xl font-semibold">
            {habito.rachaActual} <span className="font-sans text-sm font-normal text-suave">{habito.rachaActual === 1 ? 'día' : 'días'}</span>
          </dd>
        </div>
        <div className="rounded-2xl bg-[linear-gradient(140deg,color-mix(in_oklab,var(--acento)_12%,transparent),transparent)] p-4 ring-1 ring-borde/70">
          <dt className="flex items-center gap-1.5 text-xs text-suave">
            <Trophy className="size-4 text-acento" aria-hidden="true" />
            Mejor racha
          </dt>
          <dd className="titulo numeros text-3xl font-semibold">
            {habito.mejorRacha} <span className="font-sans text-sm font-normal text-suave">{habito.mejorRacha === 1 ? 'día' : 'días'}</span>
          </dd>
        </div>
      </dl>

      {habito.programadoHoy ? (
        <div className="flex flex-col gap-3 rounded-2xl bg-superficie-suave/70 p-4">
          <p className="text-sm font-medium">Hoy</p>
          <BotonesRegistro habito={habito} />
        </div>
      ) : (
        <p className="rounded-2xl bg-superficie-suave/70 p-4 text-sm text-suave">Hoy no está programado.</p>
      )}

      {hoy ? (
        <CalendarioMes habito={habito} hoy={hoy} />
      ) : historial.isError ? (
        <EstadoError error={historial.error} alReintentar={historial.refetch} titulo="No se pudo cargar el historial" />
      ) : (
        <div className="esqueleto h-64 rounded-2xl" role="status">
          <span className="sr-only">Cargando el historial…</span>
        </div>
      )}
      <LeyendaDias />

      {(alEditar || alArchivar || alEliminar) && (
        <div className="flex flex-wrap gap-2 border-t border-vidrio-borde pt-5">
          {alEditar && (
            <Boton variante="secundario" tamano="sm" onClick={alEditar}>
              <Pencil className="size-4" aria-hidden="true" />
              Editar
            </Boton>
          )}
          {alArchivar && (
            <Boton variante="secundario" tamano="sm" onClick={alArchivar}>
              <Archive className="size-4" aria-hidden="true" />
              Archivar
            </Boton>
          )}
          {alEliminar && (
            <Boton variante="fantasma" tamano="sm" onClick={alEliminar} className="text-peligro hover:bg-peligro-suave hover:text-peligro">
              <Trash2 className="size-4" aria-hidden="true" />
              Eliminar
            </Boton>
          )}
        </div>
      )}
    </div>
  );
}
