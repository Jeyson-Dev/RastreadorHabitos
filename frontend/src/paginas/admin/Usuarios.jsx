import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Users } from 'lucide-react';
import { cambiarRol, CLAVES_ADMIN, desactivarUsuario, forzarRestablecimiento, reactivarUsuario, ROLES } from '../../api/admin';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Cargando from '../../componentes/Cargando';
import Confirmar from '../../componentes/Confirmar';
import EstadoError from '../../componentes/EstadoError';
import EstadoVacio from '../../componentes/EstadoVacio';
import { useSesion } from '../../sesion/contextoSesion';
import { useUsuarios } from './consultas';

const CHIP_ESTADO = {
  Activo: 'bg-acento-suave text-acento',
  Desactivado: 'bg-peligro-suave text-peligro',
  'Pendiente de activación': 'bg-aviso-suave text-aviso',
};

// Cada acción con su texto de confirmación. Sobre uno mismo no se ofrecen las que la API rechaza.
const ACCIONES = {
  hacerAdmin: { texto: 'Hacer administrador', titulo: '¿Hacer administrador?', ejecutar: (u) => cambiarRol(u.id, ROLES.Administrador), hecho: 'ahora es administrador' },
  quitarAdmin: { texto: 'Quitar administrador', titulo: '¿Quitar el rol de administrador?', ejecutar: (u) => cambiarRol(u.id, ROLES.Estandar), hecho: 'ahora es Estándar' },
  desactivar: { texto: 'Desactivar', titulo: '¿Desactivar la cuenta?', ejecutar: (u) => desactivarUsuario(u.id), hecho: 'quedó desactivado y se cerraron sus sesiones', peligroso: true },
  reactivar: { texto: 'Reactivar', titulo: '¿Reactivar la cuenta?', ejecutar: (u) => reactivarUsuario(u.id), hecho: 'quedó reactivado' },
  restablecer: {
    texto: 'Restablecer contraseña',
    titulo: '¿Forzar el restablecimiento de la contraseña?',
    ejecutar: (u) => forzarRestablecimiento(u.id),
    hecho: 'recibirá por correo un código para definir una nueva contraseña',
    peligroso: true,
  },
};

export default function Usuarios() {
  const clienteConsultas = useQueryClient();
  const usuarios = useUsuarios();
  const { usuario: yo } = useSesion();
  const [filtro, setFiltro] = useState('');
  const [pendiente, setPendiente] = useState(null);
  const [resultado, setResultado] = useState(null);

  const accion = useMutation({
    mutationFn: ({ clave, usuario }) => ACCIONES[clave].ejecutar(usuario),
    onSuccess: (_d, { clave, usuario }) => {
      setPendiente(null);
      setResultado(`${usuario.nombreCompleto} ${ACCIONES[clave].hecho}.`);
      clienteConsultas.invalidateQueries({ queryKey: CLAVES_ADMIN.usuarios });
    },
  });

  const texto = filtro.trim().toLowerCase();
  const visibles = (usuarios.data ?? []).filter((u) => !texto || `${u.nombreCompleto} ${u.email}`.toLowerCase().includes(texto));

  let contenido;
  if (usuarios.isPending) contenido = <Cargando texto="Cargando usuarios…" />;
  else if (usuarios.isError) contenido = <EstadoError error={usuarios.error} alReintentar={usuarios.refetch} />;
  else if (visibles.length === 0) contenido = <EstadoVacio icono={Users} titulo="Ningún usuario coincide con la búsqueda" />;
  else {
    contenido = (
      <ul className="flex flex-col divide-y divide-borde overflow-hidden vidrio rounded-[1.25rem]">
        {visibles.map((u) => {
          const soyYo = u.id === yo?.id;
          const esAdmin = u.rol === ROLES.Administrador;
          const claves = soyYo ? ['restablecer'] : [esAdmin ? 'quitarAdmin' : 'hacerAdmin', u.estado === 'Desactivado' ? 'reactivar' : 'desactivar', 'restablecer'];
          return (
            <li key={u.id} className="flex flex-col gap-3 px-4 py-4 sm:px-5 lg:flex-row lg:items-center lg:justify-between">
              <div className="flex min-w-0 flex-col gap-1">
                <p className="flex flex-wrap items-center gap-2 font-medium">
                  {u.nombreCompleto}
                  {soyYo && <span className="rounded-full bg-superficie-suave px-2 py-0.5 text-xs font-medium text-suave">Tú</span>}
                </p>
                <p className="text-sm break-all text-suave">{u.email}</p>
                <p className="flex flex-wrap gap-2 text-xs">
                  <span className="rounded-full bg-superficie-suave px-2 py-0.5 font-medium">{esAdmin ? 'Administrador' : 'Estándar'}</span>
                  <span className={`rounded-full px-2 py-0.5 font-medium ${CHIP_ESTADO[u.estado] ?? 'bg-superficie-suave text-suave'}`}>{u.estado}</span>
                </p>
              </div>
              <div className="flex flex-wrap gap-2" role="group" aria-label={`Acciones sobre ${u.nombreCompleto}`}>
                {claves.map((clave) => (
                  <Boton key={clave} tamano="sm" variante="secundario" onClick={() => setPendiente({ clave, usuario: u })}>
                    {ACCIONES[clave].texto}
                  </Boton>
                ))}
              </div>
            </li>
          );
        })}
      </ul>
    );
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <h2 className="font-semibold">
          Usuarios {usuarios.data && <span className="numeros text-sm font-normal text-suave">({usuarios.data.length})</span>}
        </h2>
        <label className="flex w-full flex-col gap-1 text-xs font-medium text-suave sm:w-72">
          Buscar por nombre o correo
          <input
            type="search"
            value={filtro}
            onChange={(e) => setFiltro(e.target.value)}
            className="h-9 rounded-xl border border-borde bg-superficie/80 px-3 text-sm text-texto focus:border-acento focus:ring-2 focus:ring-acento/30 focus:outline-none"
          />
        </label>
      </div>
      {resultado && <Aviso tipo="exito">{resultado}</Aviso>}
      {contenido}
      <Confirmar
        abierto={Boolean(pendiente)}
        alCerrar={() => setPendiente(null)}
        titulo={pendiente ? ACCIONES[pendiente.clave].titulo : ''}
        descripcion={pendiente && `${pendiente.usuario.nombreCompleto} (${pendiente.usuario.email})`}
        textoBoton={pendiente ? ACCIONES[pendiente.clave].texto : ''}
        peligroso={pendiente && ACCIONES[pendiente.clave].peligroso}
        accion={accion}
        alConfirmar={() => accion.mutate(pendiente)}
      />
    </div>
  );
}
