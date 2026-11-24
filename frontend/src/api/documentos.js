import { pedir } from './cliente';

// Manejador de documentos: cada uno ve los suyos; el Administrador, todos. Eliminar es lógico.
export const CLAVES_DOCUMENTOS = { lista: ['documentos'] };

export const listarDocumentos = (senal) => pedir('/api/documentos', { senal });

export function subirDocumento(archivo) {
  const formulario = new FormData();
  formulario.append('archivo', archivo);
  return pedir('/api/documentos', { metodo: 'POST', cuerpo: formulario });
}

export const descargarDocumento = (id) => pedir(`/api/documentos/${encodeURIComponent(id)}/descarga`, { comoArchivo: true });

export const eliminarDocumento = (id) => pedir(`/api/documentos/${encodeURIComponent(id)}`, { metodo: 'DELETE' });
