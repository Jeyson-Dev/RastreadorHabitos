import { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { History } from 'lucide-react';
import { CLAVES_ADMIN, consultarAuditoria, ENTIDADES_AUDITADAS } from '../../api/admin';
import Cargando from '../../componentes/Cargando';
import EstadoError from '../../componentes/EstadoError';
import EstadoVacio from '../../componentes/EstadoVacio';
import { fechaHoraLocal } from '../../utilidades/tiempo';
import { useNombresUsuarios, useUsuarios } from './consultas';

const NOMBRE_ENTIDAD = { Usuario: 'Usuario', SolicitudPermiso: 'Solicitud de permiso', Documento: 'Documento' };
const VACIO = { usuarioId: '', entidad: '', desde: '', hasta: '' };

// Un valor anterior o nuevo llega como texto (normalmente JSON): se muestra con formato si se puede.
function legible(valor) {
  if (!valor) return '—';
  try {
    return JSON.stringify(JSON.parse(valor), null, 2);
  } catch {
    return valor;
  }
}

// Historial de auditoría (solo lectura) con filtros por usuario, entidad y fechas.
export default function Auditoria() {
  const usuarios = useUsuarios();
  const nombre = useNombresUsuarios();
  const [borrador, setBorrador] = useState(VACIO);
  const [filtros, setFiltros] = useState(VACIO);
  const [errorFiltro, setErrorFiltro] = useState(null);
  const historial = useQuery({
    queryKey: CLAVES_ADMIN.auditoria(filtros),
    queryFn: ({ signal }) => consultarAuditoria(filtros, signal),
    placeholderData: keepPreviousData,
  });

  function aplicar(evento) {
    evento.preventDefault();
    if (borrador.desde && borrador.hasta && borrador.desde > borrador.hasta) {
      setErrorFiltro('La fecha «desde» no puede ser posterior a «hasta».');
      return;
    }
    setErrorFiltro(null);
    setFiltros(borrador);
  }

  const campo = (clave) => ({ value: borrador[clave], onChange: (e) => setBorrador((b) => ({ ...b, [clave]: e.target.value })) });
  const estiloCampo = 'h-9 rounded-lg border border-borde bg-superficie px-2 text-sm text-texto focus:border-acento focus:ring-2 focus:ring-acento/30 focus:outline-none';

  let contenido;
  if (historial.isPending) contenido = <Cargando texto="Cargando el historial…" />;
  else if (historial.isError) contenido = <EstadoError error={historial.error} alReintentar={historial.refetch} />;
  else if (historial.data.registros.length === 0) contenido = <EstadoVacio icono={History} titulo="No hay registros con esos filtros" />;
  else {
    contenido = (
      <ul className={`flex flex-col divide-y divide-borde overflow-hidden rounded-xl border border-borde bg-superficie ${historial.isPlaceholderData ? 'opacity-60' : ''}`}>
        {historial.data.registros.map((r) => (
          <li key={r.id} className="flex flex-col gap-1.5 px-4 py-3.5 sm:px-5">
            <div className="flex flex-wrap items-baseline justify-between gap-2">
              <p className="font-medium">{r.accion}</p>
              <time dateTime={r.fechaUtc} className="numeros text-xs text-suave">
                {fechaHoraLocal(r.fechaUtc)}
              </time>
            </div>
            <p className="text-sm text-suave">
              {NOMBRE_ENTIDAD[r.entidad] ?? r.entidad} <span className="font-mono text-xs">{String(r.entidadId).slice(0, 8)}…</span> · por {nombre(r.usuarioId)}
            </p>
            {(r.valorAnterior || r.valorNuevo) && (
              <details>
                <summary className="cursor-pointer text-xs font-medium text-acento select-none hover:underline">Ver el cambio</summary>
                <div className="mt-2 grid gap-2 sm:grid-cols-2">
                  {[
                    ['Antes', r.valorAnterior],
                    ['Después', r.valorNuevo],
                  ].map(([titulo, valor]) => (
                    <div key={titulo} className="flex flex-col gap-1">
                      <p className="text-xs font-medium text-suave">{titulo}</p>
                      <pre className="overflow-x-auto rounded-lg bg-superficie-suave p-2 text-xs">{legible(valor)}</pre>
                    </div>
                  ))}
                </div>
              </details>
            )}
          </li>
        ))}
      </ul>
    );
  }

  return (
    <div className="flex flex-col gap-5">
      <form onSubmit={aplicar} noValidate className="flex flex-wrap items-end gap-3 rounded-xl border border-borde bg-superficie p-4">
        <label className="flex min-w-48 flex-1 flex-col gap-1 text-xs font-medium text-suave">
          Usuario
          <select {...campo('usuarioId')} className={estiloCampo}>
            <option value="">Todos</option>
            {(usuarios.data ?? []).map((u) => (
              <option key={u.id} value={u.id}>
                {u.nombreCompleto} ({u.email})
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1 text-xs font-medium text-suave">
          Entidad
          <select {...campo('entidad')} className={estiloCampo}>
            <option value="">Todas</option>
            {ENTIDADES_AUDITADAS.map((e) => (
              <option key={e} value={e}>
                {NOMBRE_ENTIDAD[e]}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1 text-xs font-medium text-suave">
          Desde
          <input type="date" {...campo('desde')} aria-invalid={errorFiltro ? true : undefined} className={estiloCampo} />
        </label>
        <label className="flex flex-col gap-1 text-xs font-medium text-suave">
          Hasta
          <input type="date" {...campo('hasta')} aria-invalid={errorFiltro ? true : undefined} className={estiloCampo} />
        </label>
        <button type="submit" className="h-9 rounded-lg bg-acento px-4 text-sm font-medium text-sobre-acento hover:bg-acento-hover">
          Filtrar
        </button>
        {errorFiltro && (
          <p role="alert" className="w-full text-xs font-medium text-peligro">
            {errorFiltro}
          </p>
        )}
      </form>
      {historial.data && (
        <p className="numeros text-sm text-suave" aria-live="polite">
          {historial.data.total} {historial.data.total === 1 ? 'registro' : 'registros'}
        </p>
      )}
      {contenido}
    </div>
  );
}
