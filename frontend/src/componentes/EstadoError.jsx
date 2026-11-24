import { RefreshCw } from 'lucide-react';
import { mensajeDeError } from '../api/cliente';
import Aviso from './Aviso';
import Boton from './Boton';

// Error al cargar datos: el mensaje comprensible y una forma de reintentar.
export default function EstadoError({ error, alReintentar, titulo = 'No se pudo cargar la información' }) {
  return (
    <div className="flex flex-col items-start gap-3">
      <Aviso tipo="error" titulo={titulo}>
        {mensajeDeError(error)}
      </Aviso>
      {alReintentar && (
        <Boton variante="secundario" tamano="sm" onClick={alReintentar}>
          <RefreshCw className="size-4" aria-hidden="true" />
          Reintentar
        </Boton>
      )}
    </div>
  );
}
