import { Bar, BarChart, CartesianGrid, Legend, Rectangle, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { etiquetaGrupo, SERIES_SOLICITUDES } from './rangos';

const EJE = { stroke: 'var(--texto-suave)', fontSize: 12, tickLine: false, axisLine: false };
const CURSOR = { fill: 'var(--superficie-suave)' };

function CajaTooltip({ active, payload, label, formato }) {
  if (!active || !payload?.length) return null;
  return (
    <div className="rounded-lg border border-borde bg-superficie px-3 py-2 text-xs shadow-sm">
      <p className="mb-1 font-medium text-texto">{label}</p>
      <ul className="flex flex-col gap-0.5">
        {payload.map((p) => (
          <li key={p.dataKey} className="flex items-center gap-2 text-suave">
            <span className="size-2 rounded-sm" style={{ background: p.color }} aria-hidden="true" />
            <span>{p.name}:</span>
            <span className="numeros font-medium text-texto">{formato ? formato(p.value, p.payload) : p.value}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

function Leyenda({ payload }) {
  return (
    <ul className="flex flex-wrap justify-center gap-x-4 gap-y-1 pt-2 text-xs text-suave">
      {payload.map((p) => (
        <li key={p.value} className="inline-flex items-center gap-1.5">
          <span className="size-2.5 rounded-sm" style={{ background: p.color }} aria-hidden="true" />
          {p.value}
        </li>
      ))}
    </ul>
  );
}

// Solicitudes por estado y mes: barras apiladas con 2 px de separación entre segmentos.
export function GraficaSolicitudes({ filas, conAnio }) {
  // El extremo redondeado va en el segmento superior con valor de cada mes, no en una serie fija.
  const datos = filas.map((f) => ({
    grupo: etiquetaGrupo(f.grupo, conAnio),
    ...f.valores,
    tope: [...SERIES_SOLICITUDES].reverse().find((s) => (f.valores[s.clave] ?? 0) > 0)?.clave,
  }));
  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart data={datos} margin={{ top: 8, right: 8, left: -16, bottom: 0 }} accessibilityLayer>
        <CartesianGrid vertical={false} stroke="var(--rejilla)" />
        <XAxis dataKey="grupo" {...EJE} />
        <YAxis allowDecimals={false} {...EJE} />
        <Tooltip cursor={CURSOR} content={<CajaTooltip />} />
        <Legend content={<Leyenda />} />
        {SERIES_SOLICITUDES.map((s) => (
          <Bar
            key={s.clave}
            dataKey={s.clave}
            name={s.texto}
            stackId="estados"
            fill={s.color}
            stroke="var(--superficie)"
            strokeWidth={2}
            maxBarSize={48}
            shape={(props) => <Rectangle {...props} radius={props.payload?.tope === s.clave ? [4, 4, 0, 0] : 0} />}
          />
        ))}
      </BarChart>
    </ResponsiveContainer>
  );
}

// Cumplimiento semanal: una serie (porcentaje de días programados cumplidos), sin leyenda.
export function GraficaCumplimiento({ filas, conAnio }) {
  const datos = filas.map((f) => ({ grupo: etiquetaGrupo(f.grupo, conAnio), ...f.valores }));
  const formato = (valor, fila) => `${Math.round(valor)} % (${fila.diasCumplidos} de ${fila.diasProgramados} días)`;
  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart data={datos} margin={{ top: 8, right: 8, left: -16, bottom: 0 }} accessibilityLayer>
        <CartesianGrid vertical={false} stroke="var(--rejilla)" />
        <XAxis dataKey="grupo" {...EJE} />
        <YAxis domain={[0, 100]} ticks={[0, 25, 50, 75, 100]} tickFormatter={(v) => `${v} %`} {...EJE} />
        <Tooltip cursor={CURSOR} content={<CajaTooltip formato={formato} />} />
        <Bar dataKey="porcentajeCumplimiento" name="Cumplimiento" fill="var(--acento)" radius={[4, 4, 0, 0]} maxBarSize={40} />
      </BarChart>
    </ResponsiveContainer>
  );
}
