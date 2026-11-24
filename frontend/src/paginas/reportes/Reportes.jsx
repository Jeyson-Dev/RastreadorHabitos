import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { claveReporte, consultarReporte, REPORTES } from '../../api/reportes';
import { GraficaCumplimiento, GraficaSolicitudes } from './Graficas';
import { etiquetaGrupo, RANGOS, rangoPredefinido, SERIES_SOLICITUDES } from './rangos';
import TarjetaReporte from './TarjetaReporte';

function useReporte(reporte, rango) {
  return useQuery({
    queryKey: claveReporte(reporte, rango.desde, rango.hasta),
    queryFn: ({ signal }) => consultarReporte(reporte, rango, signal),
    placeholderData: (anterior) => anterior,
  });
}

// Reportes con un rango de fechas común: solicitudes de permiso por estado y mes, y cumplimiento
// semanal de los hábitos. El alcance (solo sus datos o todos los usuarios) lo decide la API por rol.
export default function Reportes() {
  const [rango, setRango] = useState(() => rangoPredefinido('3m'));
  const [borrador, setBorrador] = useState(rango);
  const [errorRango, setErrorRango] = useState(null);
  const solicitudes = useReporte(REPORTES.solicitudes, rango);
  const conAnio = rango.desde.slice(0, 4) !== rango.hasta.slice(0, 4);
  const cumplimiento = useReporte(REPORTES.cumplimiento, rango);

  function elegir(id) {
    const nuevo = rangoPredefinido(id);
    setRango(nuevo);
    setBorrador(nuevo);
    setErrorRango(null);
  }

  function aplicarPersonalizado(evento) {
    evento.preventDefault();
    if (!borrador.desde || !borrador.hasta) return setErrorRango('Elige las dos fechas.');
    if (borrador.desde > borrador.hasta) return setErrorRango('La fecha «desde» no puede ser posterior a «hasta».');
    setErrorRango(null);
    setRango({ id: 'personalizado', desde: borrador.desde, hasta: borrador.hasta });
  }

  return (
    <div className="flex flex-col gap-6">
      <form onSubmit={aplicarPersonalizado} noValidate className="flex flex-wrap items-end gap-3 rounded-xl border border-borde bg-superficie p-4">
        <div role="group" aria-label="Rango rápido" className="inline-flex flex-wrap rounded-lg border border-borde p-0.5">
          {RANGOS.map((r) => (
            <button
              key={r.id}
              type="button"
              aria-pressed={rango.id === r.id}
              onClick={() => elegir(r.id)}
              className={`rounded-md px-3 py-1.5 text-sm font-medium ${rango.id === r.id ? 'bg-superficie-suave text-texto' : 'text-suave hover:text-texto'}`}
            >
              {r.texto}
            </button>
          ))}
        </div>
        <div className="flex flex-wrap items-end gap-2">
          {[
            ['desde', 'Desde'],
            ['hasta', 'Hasta'],
          ].map(([campo, texto]) => (
            <label key={campo} className="flex flex-col gap-1 text-xs font-medium text-suave">
              {texto}
              <input
                type="date"
                value={borrador[campo]}
                onChange={(e) => setBorrador((b) => ({ ...b, [campo]: e.target.value }))}
                aria-invalid={errorRango ? true : undefined}
                className="h-9 rounded-lg border border-borde bg-superficie px-2 text-sm text-texto focus:border-acento focus:ring-2 focus:ring-acento/30 focus:outline-none"
              />
            </label>
          ))}
          <button type="submit" className="h-9 rounded-lg border border-borde px-3 text-sm font-medium hover:bg-superficie-suave">
            Aplicar
          </button>
        </div>
        {errorRango && (
          <p role="alert" className="w-full text-xs font-medium text-peligro">
            {errorRango}
          </p>
        )}
      </form>

      <TarjetaReporte
        id="reporte-cumplimiento"
        titulo="Cumplimiento semanal"
        consulta={cumplimiento}
        sinDatos="Registra tus hábitos para ver qué porcentaje de los días programados cumples cada semana."
        grafica={cumplimiento.data && <GraficaCumplimiento filas={cumplimiento.data.filas} conAnio={conAnio} />}
        tabla={
          cumplimiento.data && (
            <Tabla
              columnas={['Semana del', 'Días programados', 'Días cumplidos', 'Cumplimiento']}
              filas={cumplimiento.data.filas.map((f) => [
                etiquetaGrupo(f.grupo, conAnio),
                f.valores.diasProgramados,
                f.valores.diasCumplidos,
                `${Math.round(f.valores.porcentajeCumplimiento)} %`,
              ])}
            />
          )
        }
      />
      <TarjetaReporte
        id="reporte-solicitudes"
        titulo="Solicitudes de permiso por estado"
        consulta={solicitudes}
        sinDatos="Aquí verás cuántas solicitudes de permiso hubo cada mes y en qué estado quedaron."
        grafica={solicitudes.data && <GraficaSolicitudes filas={solicitudes.data.filas} conAnio={conAnio} />}
        tabla={
          solicitudes.data && (
            <Tabla
              columnas={['Mes', ...SERIES_SOLICITUDES.map((s) => s.texto), 'Total']}
              filas={solicitudes.data.filas.map((f) => [
                etiquetaGrupo(f.grupo, conAnio),
                ...SERIES_SOLICITUDES.map((s) => f.valores[s.clave] ?? 0),
                f.valores.Total ?? 0,
              ])}
            />
          )
        }
      />
    </div>
  );
}

function Tabla({ columnas, filas }) {
  return (
    <table className="numeros w-full text-left text-sm">
      <thead>
        <tr className="border-b border-borde text-xs text-suave">
          {columnas.map((c, i) => (
            <th key={c} scope="col" className={`py-2 pr-4 font-medium ${i > 0 ? 'text-right' : ''}`}>
              {c}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {filas.map((fila, i) => (
          <tr key={`${i}-${fila[0]}`} className="border-b border-borde/60 last:border-0">
            {fila.map((celda, i) =>
              i === 0 ? (
                <th key={i} scope="row" className="py-2 pr-4 font-medium">
                  {celda}
                </th>
              ) : (
                <td key={i} className="py-2 pr-4 text-right">
                  {celda}
                </td>
              ),
            )}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
