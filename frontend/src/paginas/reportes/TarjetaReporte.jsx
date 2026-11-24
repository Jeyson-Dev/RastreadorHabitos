import { ChartColumn } from 'lucide-react';
import Cargando from '../../componentes/Cargando';
import EstadoError from '../../componentes/EstadoError';
import EstadoVacio from '../../componentes/EstadoVacio';
import Tarjeta from '../../componentes/Tarjeta';

// Marco común de un reporte: título, la pregunta que responde y el alcance (por rol), los estados
// de carga/error/vacío, la gráfica y su tabla de datos (la gráfica nunca es la única forma de leerlo).
export default function TarjetaReporte({ id, titulo, consulta, sinDatos, grafica, tabla }) {
  const datos = consulta.data;
  return (
    <Tarjeta
      aria-labelledby={id}
      aria-busy={consulta.isFetching || undefined}
      className={`flex flex-col gap-4 transition-opacity ${consulta.isPlaceholderData ? 'opacity-60' : ''}`}
    >
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="flex min-w-0 flex-col gap-0.5">
          <h2 id={id} className="font-semibold">
            {titulo}
          </h2>
          {datos?.pregunta && <p className="text-sm text-suave">{datos.pregunta}</p>}
        </div>
        {datos?.alcance && (
          <span className="shrink-0 rounded-full bg-superficie-suave px-2.5 py-0.5 text-xs font-medium text-suave">{datos.alcance}</span>
        )}
      </div>
      {consulta.isPending ? (
        <Cargando texto="Calculando el reporte…" />
      ) : consulta.isError ? (
        <EstadoError error={consulta.error} alReintentar={consulta.refetch} titulo="No se pudo generar el reporte" />
      ) : datos.filas.length === 0 ? (
        <EstadoVacio icono={ChartColumn} titulo="No hay datos en este rango">
          {sinDatos}
        </EstadoVacio>
      ) : (
        <>
          <div className="h-72 w-full">{grafica}</div>
          <details className="group">
            <summary className="cursor-pointer text-sm font-medium text-acento select-none hover:underline">Ver los datos en una tabla</summary>
            <div className="mt-3 overflow-x-auto">{tabla}</div>
          </details>
        </>
      )}
    </Tarjeta>
  );
}
