import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Check, KeyRound, X } from 'lucide-react';
import { aprobarSolicitud, CLAVES_SOLICITUDES, listarSolicitudesPendientes, rechazarSolicitud } from '../../api/solicitudes';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Cargando from '../../componentes/Cargando';
import Confirmar from '../../componentes/Confirmar';
import EstadoError from '../../componentes/EstadoError';
import EstadoVacio from '../../componentes/EstadoVacio';
import { useSesion } from '../../sesion/contextoSesion';
import { fechaHoraLocal, haceCuanto } from '../../utilidades/tiempo';
import { useNombresUsuarios } from './consultas';

const NOMBRES_PERMISO = { LIMITE_HABITOS_AMPLIADO: 'Límite de hábitos ampliado (20)', Administrador: 'Rol Administrador' };

// Solicitudes de permiso pendientes de todos los usuarios. Aprobar aplica el permiso al instante;
// rechazar exige un motivo. Nadie resuelve su propia solicitud (la API también lo impide).
export default function Solicitudes() {
  const clienteConsultas = useQueryClient();
  const { usuario } = useSesion();
  const nombre = useNombresUsuarios();
  const pendientes = useQuery({ queryKey: CLAVES_SOLICITUDES.pendientes, queryFn: ({ signal }) => listarSolicitudesPendientes(signal) });
  const [dialogo, setDialogo] = useState(null);
  const [resultado, setResultado] = useState(null);

  const alTerminar = (texto) => {
    setDialogo(null);
    setResultado(texto);
    clienteConsultas.invalidateQueries({ queryKey: CLAVES_SOLICITUDES.pendientes });
    clienteConsultas.invalidateQueries({ queryKey: ['notificaciones'] });
  };
  const aprobar = useMutation({ mutationFn: (s) => aprobarSolicitud(s.id), onSuccess: (_d, s) => alTerminar(`Aprobaste la solicitud de ${nombre(s.solicitanteId)}.`) });
  const rechazar = useMutation({
    mutationFn: ({ s, motivo }) => rechazarSolicitud(s.id, motivo),
    onSuccess: (_d, { s }) => alTerminar(`Rechazaste la solicitud de ${nombre(s.solicitanteId)}.`),
  });

  let contenido;
  if (pendientes.isPending) contenido = <Cargando texto="Cargando solicitudes…" />;
  else if (pendientes.isError) contenido = <EstadoError error={pendientes.error} alReintentar={pendientes.refetch} />;
  else if (pendientes.data.length === 0) {
    contenido = (
      <EstadoVacio icono={KeyRound} titulo="No hay solicitudes pendientes">
        Cuando alguien pida un permiso, aparecerá aquí y recibirás una notificación.
      </EstadoVacio>
    );
  } else {
    contenido = (
      <ul className="flex flex-col divide-y divide-borde overflow-hidden vidrio rounded-[1.25rem]">
        {pendientes.data.map((s) => {
          const propia = s.solicitanteId === usuario?.id;
          return (
            <li key={s.id} className="flex flex-wrap items-center justify-between gap-3 px-4 py-4 sm:px-5">
              <div className="flex min-w-0 flex-col gap-0.5">
                <p className="font-medium">{NOMBRES_PERMISO[s.permiso] ?? s.permiso}</p>
                <p className="text-sm text-suave">{nombre(s.solicitanteId)}</p>
                <time dateTime={s.fechaCreacionUtc} title={fechaHoraLocal(s.fechaCreacionUtc)} className="text-xs text-suave">
                  Pedida {haceCuanto(s.fechaCreacionUtc)}
                </time>
              </div>
              {propia ? (
                <p className="text-sm text-suave">Es tuya: debe resolverla otro administrador.</p>
              ) : (
                <div className="flex gap-2">
                  <Boton tamano="sm" onClick={() => setDialogo({ tipo: 'aprobar', s })}>
                    <Check className="size-4" aria-hidden="true" />
                    Aprobar
                  </Boton>
                  <Boton tamano="sm" variante="secundario" onClick={() => setDialogo({ tipo: 'rechazar', s })}>
                    <X className="size-4" aria-hidden="true" />
                    Rechazar
                  </Boton>
                </div>
              )}
            </li>
          );
        })}
      </ul>
    );
  }

  return (
    <div className="flex flex-col gap-5">
      <h2 className="font-semibold">Pendientes de resolver</h2>
      {resultado && <Aviso tipo="exito">{resultado}</Aviso>}
      {contenido}
      <Confirmar
        abierto={dialogo?.tipo === 'aprobar'}
        alCerrar={() => setDialogo(null)}
        titulo="¿Aprobar la solicitud?"
        descripcion={dialogo && `${NOMBRES_PERMISO[dialogo.s.permiso] ?? dialogo.s.permiso} para ${nombre(dialogo.s.solicitanteId)}. El permiso se aplica de inmediato.`}
        textoBoton="Aprobar"
        accion={aprobar}
        alConfirmar={() => aprobar.mutate(dialogo.s)}
      />
      <Confirmar
        abierto={dialogo?.tipo === 'rechazar'}
        alCerrar={() => setDialogo(null)}
        titulo="¿Rechazar la solicitud?"
        descripcion={dialogo && `El solicitante recibirá el motivo en su notificación.`}
        textoBoton="Rechazar"
        peligroso
        pedirMotivo="Motivo del rechazo"
        accion={rechazar}
        alConfirmar={(motivo) => rechazar.mutate({ s: dialogo.s, motivo })}
      />
    </div>
  );
}
