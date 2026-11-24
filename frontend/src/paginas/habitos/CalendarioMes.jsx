import { useState } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import {
  DIAS_SEMANA,
  diaSemana,
  fechaLarga,
  nombreMes,
  primerDiaDelMes,
  rangoDias,
  sumarMeses,
  ultimoDiaDelMes,
} from '../../utilidades/fechas';
import { useRegistros } from './consultas';
import { diaDeCreacion, ESTADOS_DIA, estadoDelDia, mapaRegistros } from './estadoDia';

// Calendario mensual del historial de un hábito, navegable desde el mes de creación hasta el actual.
// «hoy» es el día del servidor (viene del historial de los últimos 30 días).
export default function CalendarioMes({ habito, hoy }) {
  const mesActual = primerDiaDelMes(hoy);
  const creacion = diaDeCreacion(habito);
  const mesCreacion = creacion ? primerDiaDelMes(creacion) : mesActual;
  const [mes, setMes] = useState(mesActual);
  const historial = useRegistros(habito.id, { desde: mes, hasta: ultimoDiaDelMes(mes) });

  const dias = rangoDias(mes, ultimoDiaDelMes(mes));
  const huecos = diaSemana(mes) - 1;
  const contexto = { habito, registros: mapaRegistros(historial.data), hoy, creacion };

  return (
    <section aria-label="Calendario del mes" className="flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <button
          type="button"
          onClick={() => setMes(sumarMeses(mes, -1))}
          disabled={mes <= mesCreacion}
          className="inline-flex size-9 items-center justify-center rounded-lg text-suave hover:bg-superficie-suave disabled:opacity-30"
          aria-label="Mes anterior"
        >
          <ChevronLeft className="size-5" aria-hidden="true" />
        </button>
        <p className="font-medium" aria-live="polite">
          {nombreMes(mes)}
        </p>
        <button
          type="button"
          onClick={() => setMes(sumarMeses(mes, 1))}
          disabled={mes >= mesActual}
          className="inline-flex size-9 items-center justify-center rounded-lg text-suave hover:bg-superficie-suave disabled:opacity-30"
          aria-label="Mes siguiente"
        >
          <ChevronRight className="size-5" aria-hidden="true" />
        </button>
      </div>

      <div className="grid grid-cols-7 gap-1 text-center text-xs text-suave" aria-hidden="true">
        {DIAS_SEMANA.map((d) => (
          <span key={d.numero}>{d.corto}</span>
        ))}
      </div>
      {historial.isError ? (
        <p className="text-sm text-suave">No se pudo cargar este mes.</p>
      ) : (
        <ol className={`grid grid-cols-7 gap-1 ${historial.isPending ? 'animate-pulse' : ''}`}>
          {Array.from({ length: huecos }, (_, i) => (
            <li key={`hueco-${i}`} aria-hidden="true" />
          ))}
          {dias.map((dia) => {
            const estado = historial.isPending ? 'fuera' : estadoDelDia(dia, contexto);
            return (
              <li key={dia} className="flex aspect-square flex-col items-center justify-center gap-1 rounded-lg">
                <span className="sr-only">{`${fechaLarga(dia)}: ${ESTADOS_DIA[estado].texto}`}</span>
                <span className={`numeros text-xs ${dia === hoy ? 'font-bold text-texto' : 'text-suave'}`} aria-hidden="true">
                  {Number(dia.slice(8))}
                </span>
                <span className={`size-2 rounded-full ${ESTADOS_DIA[estado].clase}`} aria-hidden="true" />
              </li>
            );
          })}
        </ol>
      )}
    </section>
  );
}
