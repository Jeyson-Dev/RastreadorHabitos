import { Flame, Trophy } from 'lucide-react';
import { describirDias } from '../../utilidades/fechas';
import BotonesRegistro from './BotonesRegistro';
import CalendarioMes from './CalendarioMes';
import { useRegistros } from './consultas';
import { LeyendaDias } from './TiraDias';

// Detalle de un hábito dentro del panel lateral: rachas, registro de hoy y calendario del mes.
export default function DetalleHabito({ habito }) {
  const historial = useRegistros(habito.id);
  const hoy = historial.data?.hasta;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <p className="text-sm text-suave">{describirDias(habito.dias)}</p>
        {habito.descripcion && <p className="text-sm">{habito.descripcion}</p>}
      </div>

      <dl className="grid grid-cols-2 gap-3">
        <div className="rounded-xl border border-borde p-4">
          <dt className="flex items-center gap-1.5 text-xs text-suave">
            <Flame className="size-4 text-racha" aria-hidden="true" />
            Racha actual
          </dt>
          <dd className="numeros text-2xl font-semibold">
            {habito.rachaActual} <span className="text-sm font-normal text-suave">{habito.rachaActual === 1 ? 'día' : 'días'}</span>
          </dd>
        </div>
        <div className="rounded-xl border border-borde p-4">
          <dt className="flex items-center gap-1.5 text-xs text-suave">
            <Trophy className="size-4" aria-hidden="true" />
            Mejor racha
          </dt>
          <dd className="numeros text-2xl font-semibold">
            {habito.mejorRacha} <span className="text-sm font-normal text-suave">{habito.mejorRacha === 1 ? 'día' : 'días'}</span>
          </dd>
        </div>
      </dl>

      {habito.programadoHoy ? (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl bg-superficie-suave p-4">
          <p className="text-sm font-medium">Hoy</p>
          <BotonesRegistro habito={habito} />
        </div>
      ) : (
        <p className="rounded-xl bg-superficie-suave p-4 text-sm text-suave">Hoy no está programado.</p>
      )}

      {hoy && <CalendarioMes habito={habito} hoy={hoy} />}
      <LeyendaDias />
    </div>
  );
}
