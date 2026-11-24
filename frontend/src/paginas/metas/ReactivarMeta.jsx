import { useState } from 'react';
import { mensajeDeError } from '../../api/cliente';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Campo from '../../componentes/Campo';
import Dialogo from '../../componentes/Dialogo';
import { diaLocalDe, sumarDias } from '../../utilidades/fechas';
import { useReactivarMeta } from './consultas';

// Reactivar una meta Fallida: pide una fecha límite nueva y futura (el servidor la valida).
export default function ReactivarMeta({ meta, alCerrar }) {
  return (
    <Dialogo
      abierto={Boolean(meta)}
      alCerrar={alCerrar}
      titulo={meta ? `Reactivar «${meta.nombre}»` : ''}
      descripcion="La meta vuelve a En progreso con una fecha límite nueva. El avance sigue contando desde su inicio."
    >
      {meta && <FormularioReactivar key={meta.id} meta={meta} alCerrar={alCerrar} />}
    </Dialogo>
  );
}

function FormularioReactivar({ meta, alCerrar }) {
  const reactivar = useReactivarMeta();
  const manana = sumarDias(diaLocalDe(new Date()), 1);
  const [fecha, setFecha] = useState(manana);
  const [error, setError] = useState(null);

  function alEnviar(evento) {
    evento.preventDefault();
    if (reactivar.isPending) return;
    if (!/^\d{4}-\d{2}-\d{2}$/.test(fecha)) {
      setError('Elige la nueva fecha límite.');
      return;
    }
    if (fecha < manana) {
      setError('La nueva fecha límite debe ser posterior a hoy.');
      return;
    }
    setError(null);
    reactivar.mutate({ id: meta.id, nuevaFechaLimite: fecha }, { onSuccess: alCerrar });
  }

  return (
      <form onSubmit={alEnviar} noValidate className="flex flex-col gap-4">
        {reactivar.isError && <Aviso tipo="error">{mensajeDeError(reactivar.error)}</Aviso>}
        <Campo
          etiqueta="Nueva fecha límite"
          type="date"
          min={manana}
          value={fecha}
          onChange={(e) => setFecha(e.target.value)}
          error={error}
          data-autofocus
        />
        <div className="flex justify-end gap-2">
          <Boton variante="fantasma" onClick={alCerrar} disabled={reactivar.isPending}>
            Cancelar
          </Boton>
          <Boton type="submit" cargando={reactivar.isPending}>
            Reactivar meta
          </Boton>
        </div>
      </form>
  );
}
