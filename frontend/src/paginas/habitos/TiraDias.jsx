import { fechaLarga, rangoDias } from '../../utilidades/fechas';
import { useRegistros } from './consultas';
import { diaDeCreacion, ESTADOS_DIA, estadoDelDia, mapaRegistros } from './estadoDia';

const DIAS_TIRA = 30;

// Últimos 30 días de un hábito como una tira de puntos. Para lectores de pantalla se resume en una
// frase; cada punto lleva su fecha y estado como título.
export default function TiraDias({ habito }) {
  const historial = useRegistros(habito.id);

  if (historial.isPending) {
    return (
      <div className="grid grid-cols-[repeat(30,minmax(0,1fr))] gap-[3px]" aria-hidden="true">
        {Array.from({ length: DIAS_TIRA }, (_, i) => (
          <span key={i} className="aspect-square max-w-3 animate-pulse rounded-full bg-superficie-suave" />
        ))}
      </div>
    );
  }
  if (historial.isError) return <p className="text-xs text-suave">No se pudo cargar el historial.</p>;

  const { desde, hasta } = historial.data;
  const contexto = { habito, registros: mapaRegistros(historial.data), hoy: hasta, creacion: diaDeCreacion(habito) };
  const dias = rangoDias(desde, hasta).map((dia) => ({ dia, estado: estadoDelDia(dia, contexto) }));
  const contar = (estado) => dias.filter((d) => d.estado === estado).length;
  const resumen = `Últimos ${dias.length} días: ${contar('cumplido')} cumplidos, ${contar('no-cumplido')} no cumplidos y ${contar('sin-registro')} sin registrar.`;

  return (
    <div role="img" aria-label={resumen} className="grid grid-cols-[repeat(30,minmax(0,1fr))] gap-[3px]">
      {dias.map(({ dia, estado }) => (
        <span
          key={dia}
          title={`${fechaLarga(dia)}: ${ESTADOS_DIA[estado].texto}`}
          className={`aspect-square max-w-3 rounded-full ${ESTADOS_DIA[estado].clase}`}
        />
      ))}
    </div>
  );
}

export function LeyendaDias() {
  const estados = ['cumplido', 'no-cumplido', 'sin-registro', 'pendiente', 'no-programado'];
  return (
    <ul className="flex flex-wrap gap-x-4 gap-y-1.5 text-xs text-suave" aria-label="Leyenda del historial">
      {estados.map((estado) => (
        <li key={estado} className="inline-flex items-center gap-1.5">
          <span className={`size-2.5 rounded-full ${ESTADOS_DIA[estado].clase}`} aria-hidden="true" />
          {ESTADOS_DIA[estado].texto.replace(' (hoy)', ' hoy')}
        </li>
      ))}
    </ul>
  );
}
