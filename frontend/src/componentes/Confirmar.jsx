import { useEffect, useId, useState } from 'react';
import { mensajeDeError } from '../api/cliente';
import Aviso from './Aviso';
import Boton from './Boton';
import Dialogo from './Dialogo';

// Confirmación de una acción con consecuencias. Con «pedirMotivo», exige un texto (no vacío ni en
// blanco) que se entrega a «alConfirmar». «accion» es una mutación de TanStack Query.
export default function Confirmar({ abierto, alCerrar, titulo, descripcion, textoBoton, peligroso, pedirMotivo, accion, alConfirmar }) {
  return (
    <Dialogo abierto={abierto} alCerrar={alCerrar} titulo={titulo} descripcion={descripcion} bloqueado={accion.isPending}>
      {abierto && (
        <CuerpoConfirmar
          alCerrar={alCerrar}
          textoBoton={textoBoton}
          peligroso={peligroso}
          pedirMotivo={pedirMotivo}
          accion={accion}
          alConfirmar={alConfirmar}
        />
      )}
    </Dialogo>
  );
}

function CuerpoConfirmar({ alCerrar, textoBoton, peligroso, pedirMotivo, accion, alConfirmar }) {
  const [motivo, setMotivo] = useState('');
  const [error, setError] = useState(null);
  const idMotivo = useId();
  const idError = useId();
  const { reset } = accion;

  // Una mutación sirve a varios objetivos: el error de uno no debe verse al abrir otro.
  useEffect(() => {
    reset();
  }, [reset]);

  function alEnviar(evento) {
    evento.preventDefault();
    if (accion.isPending) return;
    if (pedirMotivo && !motivo.trim()) {
      setError('Escribe el motivo.');
      return;
    }
    setError(null);
    alConfirmar(pedirMotivo ? motivo.trim() : undefined);
  }

  return (
    <form onSubmit={alEnviar} noValidate className="flex flex-col gap-4">
      {accion.isError && <Aviso tipo="error">{mensajeDeError(accion.error)}</Aviso>}
      {pedirMotivo && (
        <div className="flex flex-col gap-1.5">
          <label htmlFor={idMotivo} className="text-sm font-medium">
            {pedirMotivo}
          </label>
          <textarea
            id={idMotivo}
            rows={3}
            maxLength={500}
            value={motivo}
            onChange={(e) => setMotivo(e.target.value)}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? idError : undefined}
            data-autofocus
            className="rounded-xl border border-borde bg-superficie/80 px-3 py-2 text-sm focus:border-acento focus:ring-2 focus:ring-acento/30 focus:outline-none"
          />
          {error && (
            <p id={idError} role="alert" className="text-xs font-medium text-peligro">
              {error}
            </p>
          )}
        </div>
      )}
      <div className="flex justify-end gap-2">
        <Boton variante="fantasma" onClick={alCerrar} disabled={accion.isPending} data-autofocus={pedirMotivo ? undefined : true}>
          Cancelar
        </Boton>
        <Boton type="submit" variante={peligroso ? 'peligro' : 'primario'} cargando={accion.isPending}>
          {textoBoton}
        </Boton>
      </div>
    </form>
  );
}
