import { useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Download, FileText, Trash2, Upload } from 'lucide-react';
import { mensajeDeError } from '../../api/cliente';
import { CLAVES_DOCUMENTOS, descargarDocumento, eliminarDocumento, listarDocumentos, subirDocumento } from '../../api/documentos';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Cargando from '../../componentes/Cargando';
import Confirmar from '../../componentes/Confirmar';
import EstadoError from '../../componentes/EstadoError';
import EstadoVacio from '../../componentes/EstadoVacio';
import { useSesion } from '../../sesion/contextoSesion';
import { fechaHoraLocal } from '../../utilidades/tiempo';

function tamano(bytes) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

// La descarga lleva el token, así que se pide con fetch y se entrega como archivo local.
async function guardarComo(documento) {
  const blob = await descargarDocumento(documento.id);
  const url = URL.createObjectURL(blob);
  const enlace = document.createElement('a');
  try {
    enlace.href = url;
    enlace.download = documento.nombre;
    document.body.append(enlace);
    enlace.click();
  } finally {
    enlace.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}

// Documentos: subir, listar, descargar y eliminar (lógico). Cada uno ve los suyos; el Administrador, todos.
export default function Documentos() {
  const clienteConsultas = useQueryClient();
  const { usuario, esAdministrador } = useSesion();
  const entrada = useRef(null);
  const [aEliminar, setAEliminar] = useState(null);
  const [aviso, setAviso] = useState(null);
  const documentos = useQuery({ queryKey: CLAVES_DOCUMENTOS.lista, queryFn: ({ signal }) => listarDocumentos(signal) });
  const recargar = () => clienteConsultas.invalidateQueries({ queryKey: CLAVES_DOCUMENTOS.lista });

  const subir = useMutation({
    mutationFn: subirDocumento,
    onSuccess: (doc) => {
      setAviso({ tipo: 'exito', texto: `Subiste «${doc.nombre}».` });
      recargar();
    },
    onError: (e) => setAviso({ tipo: 'error', texto: mensajeDeError(e) }),
  });
  const eliminar = useMutation({
    mutationFn: (doc) => eliminarDocumento(doc.id),
    onSuccess: (_d, doc) => {
      setAEliminar(null);
      setAviso({ tipo: 'exito', texto: `Eliminaste «${doc.nombre}».` });
      recargar();
    },
  });
  const descargar = useMutation({ mutationFn: guardarComo, onError: (e) => setAviso({ tipo: 'error', texto: mensajeDeError(e) }) });

  function alElegir(evento) {
    const archivo = evento.target.files?.[0];
    evento.target.value = '';
    if (!archivo) return;
    if (archivo.size === 0) {
      setAviso({ tipo: 'error', texto: 'El archivo está vacío.' });
      return;
    }
    setAviso(null);
    subir.mutate(archivo);
  }

  let contenido;
  if (documentos.isPending) contenido = <Cargando texto="Cargando documentos…" />;
  else if (documentos.isError) contenido = <EstadoError error={documentos.error} alReintentar={documentos.refetch} />;
  else if (documentos.data.length === 0) {
    contenido = (
      <EstadoVacio icono={FileText} titulo="Aún no hay documentos">
        Sube un archivo para guardarlo aquí y descargarlo cuando lo necesites.
      </EstadoVacio>
    );
  } else {
    contenido = (
      <ul className="flex flex-col divide-y divide-borde overflow-hidden rounded-xl border border-borde bg-superficie">
        {documentos.data.map((d) => (
          <li key={d.id} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3.5 sm:px-5">
            <div className="flex min-w-0 items-center gap-3">
              <FileText className="size-5 shrink-0 text-suave" aria-hidden="true" />
              <div className="flex min-w-0 flex-col">
                <p className="truncate font-medium">{d.nombre}</p>
                <p className="numeros text-xs text-suave">
                  {tamano(d.tamanoBytes)} · {fechaHoraLocal(d.fechaSubidaUtc)}
                  {esAdministrador && d.subidoPorId !== usuario?.id && ' · de otro usuario'}
                </p>
              </div>
            </div>
            <div className="flex gap-2">
              <Boton
                tamano="sm"
                variante="secundario"
                onClick={() => descargar.mutate(d)}
                cargando={descargar.isPending && descargar.variables?.id === d.id}
                disabled={descargar.isPending}
                aria-label={`Descargar ${d.nombre}`}
              >
                <Download className="size-4" aria-hidden="true" />
                <span className="hidden sm:inline">Descargar</span>
              </Boton>
              <Boton tamano="sm" variante="fantasma" onClick={() => setAEliminar(d)} aria-label={`Eliminar ${d.nombre}`}>
                <Trash2 className="size-4" aria-hidden="true" />
              </Boton>
            </div>
          </li>
        ))}
      </ul>
    );
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="font-semibold">{esAdministrador ? 'Documentos de todos los usuarios' : 'Tus documentos'}</h2>
        <input ref={entrada} type="file" className="sr-only" id="archivo-documento" onChange={alElegir} tabIndex={-1} aria-label="Elegir archivo" />
        <Boton onClick={() => entrada.current?.click()} cargando={subir.isPending}>
          <Upload className="size-4" aria-hidden="true" />
          Subir archivo
        </Boton>
      </div>
      {aviso && <Aviso tipo={aviso.tipo}>{aviso.texto}</Aviso>}
      {contenido}
      <Confirmar
        abierto={Boolean(aEliminar)}
        alCerrar={() => setAEliminar(null)}
        titulo="¿Eliminar el documento?"
        descripcion={aEliminar && `«${aEliminar.nombre}» dejará de aparecer en la lista.`}
        textoBoton="Eliminar"
        peligroso
        accion={eliminar}
        alConfirmar={() => eliminar.mutate(aEliminar)}
      />
    </div>
  );
}
