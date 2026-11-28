import { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Mail } from 'lucide-react';
import { CLAVES_ADMIN, ESTADOS_CORREO, listarCorreos } from '../../api/admin';
import Cargando from '../../componentes/Cargando';
import EstadoError from '../../componentes/EstadoError';
import EstadoVacio from '../../componentes/EstadoVacio';
import { fechaHoraLocal } from '../../utilidades/tiempo';

const CHIP = { Pendiente: 'bg-aviso-suave text-aviso', Enviado: 'bg-acento-suave text-acento', Fallido: 'bg-peligro-suave text-peligro' };

// Cola de correos (solo lectura): el enviador la procesa aparte. Filtro por estado; el cuerpo de los
// correos nunca se muestra (lleva enlaces y códigos de otros usuarios).
export default function Correos() {
  const [estado, setEstado] = useState('');
  const correos = useQuery({
    queryKey: CLAVES_ADMIN.correos(estado || null),
    queryFn: ({ signal }) => listarCorreos(estado || undefined, signal),
    placeholderData: keepPreviousData,
    refetchInterval: 15 * 1000,
  });

  let contenido;
  if (correos.isPending) contenido = <Cargando texto="Cargando la cola…" />;
  else if (correos.isError) contenido = <EstadoError error={correos.error} alReintentar={correos.refetch} />;
  else if (correos.data.length === 0) contenido = <EstadoVacio icono={Mail} titulo="No hay correos con ese estado" />;
  else {
    contenido = (
      <ul className="flex flex-col divide-y divide-borde overflow-hidden vidrio rounded-[1.25rem]">
        {correos.data.map((c) => (
          <li key={c.id} className="flex flex-col gap-2 px-4 py-4 sm:px-5">
            <div className="flex flex-wrap items-start justify-between gap-2">
              <div className="flex min-w-0 flex-col">
                <p className="font-medium">{c.asunto}</p>
                <p className="text-sm break-all text-suave">{c.destinatario}</p>
              </div>
              <span className={`shrink-0 rounded-full px-2.5 py-0.5 text-xs font-medium ${CHIP[c.estado] ?? 'bg-superficie-suave text-suave'}`}>{c.estado}</span>
            </div>
            <dl className="numeros grid grid-cols-1 gap-x-4 gap-y-1 text-xs break-all text-suave min-[420px]:grid-cols-2 sm:grid-cols-4">
              <div>
                <dt className="inline">Intentos: </dt>
                <dd className="inline text-texto">{c.intentos}</dd>
              </div>
              <div>
                <dt className="inline">Creado: </dt>
                <dd className="inline text-texto">{fechaHoraLocal(c.fechaCreacionUtc)}</dd>
              </div>
              <div>
                <dt className="inline">Último intento: </dt>
                <dd className="inline text-texto">{c.fechaUltimoIntentoUtc ? fechaHoraLocal(c.fechaUltimoIntentoUtc) : '—'}</dd>
              </div>
              <div>
                <dt className="inline">Enviado: </dt>
                <dd className="inline text-texto">{c.fechaEnvioUtc ? fechaHoraLocal(c.fechaEnvioUtc) : '—'}</dd>
              </div>
            </dl>
            {c.ultimoError && <p className="rounded-lg bg-peligro-suave px-3 py-2 text-xs break-words text-peligro">Último error: {c.ultimoError}</p>}
          </li>
        ))}
      </ul>
    );
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <h2 className="font-semibold">Cola de correos</h2>
        <label className="flex flex-col gap-1 text-xs font-medium text-suave">
          Estado
          <select
            value={estado}
            onChange={(e) => setEstado(e.target.value)}
            className="h-9 rounded-lg border border-borde bg-superficie px-2 text-sm text-texto focus:border-acento focus:ring-2 focus:ring-acento/30 focus:outline-none"
          >
            <option value="">Todos</option>
            {ESTADOS_CORREO.map((e) => (
              <option key={e} value={e}>
                {e}
              </option>
            ))}
          </select>
        </label>
      </div>
      {contenido}
    </div>
  );
}
