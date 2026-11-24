import { useState } from 'react';
import { Check, X } from 'lucide-react';
import { mensajeDeError } from '../../api/cliente';
import Boton from '../../componentes/Boton';
import { useRegistrarCumplimiento } from './consultas';

// Registro de hoy con dos decisiones explícitas: «Hecho» o «No lo hice» (esta rompe la racha). Una
// vez registrado se muestra el estado con «Cambiar», porque se puede corregir el mismo día.
export default function BotonesRegistro({ habito }) {
  const registrar = useRegistrarCumplimiento();
  const [cambiando, setCambiando] = useState(false);
  const registrado = habito.cumplidoHoy !== null && habito.cumplidoHoy !== undefined;
  const enviando = registrar.isPending ? registrar.variables?.cumplido : null;

  function marcar(cumplido) {
    registrar.mutate({ id: habito.id, cumplido }, { onSuccess: () => setCambiando(false) });
  }

  return (
    <div className="flex flex-col items-end gap-1.5">
      {registrado && !cambiando ? (
        <div className="flex items-center gap-2 text-sm">
          {habito.cumplidoHoy ? (
            <span className="inline-flex items-center gap-1 font-medium text-acento">
              <Check className="size-4" aria-hidden="true" />
              Hecho
            </span>
          ) : (
            <span className="inline-flex items-center gap-1 font-medium text-peligro">
              <X className="size-4" aria-hidden="true" />
              No lo hiciste
            </span>
          )}
          <span aria-hidden="true" className="text-borde">
            ·
          </span>
          <button
            type="button"
            onClick={() => setCambiando(true)}
            className="text-suave underline-offset-2 hover:text-texto hover:underline"
            aria-label={`Cambiar el registro de hoy de ${habito.nombre}`}
          >
            Cambiar
          </button>
        </div>
      ) : (
        <div className="flex flex-wrap justify-end gap-2" role="group" aria-label={`Registrar hoy: ${habito.nombre}`}>
          <Boton tamano="sm" onClick={() => marcar(true)} cargando={enviando === true} disabled={registrar.isPending}>
            {enviando !== true && <Check className="size-4" aria-hidden="true" />}
            Hecho
          </Boton>
          <Boton
            tamano="sm"
            variante="secundario"
            onClick={() => marcar(false)}
            cargando={enviando === false}
            disabled={registrar.isPending}
          >
            {enviando !== false && <X className="size-4" aria-hidden="true" />}
            No lo hice
          </Boton>
          {cambiando && (
            <Boton tamano="sm" variante="fantasma" onClick={() => setCambiando(false)} disabled={registrar.isPending}>
              Cancelar
            </Boton>
          )}
        </div>
      )}
      {registrar.isError && (
        <p role="alert" className="max-w-xs text-right text-xs font-medium text-peligro">
          {mensajeDeError(registrar.error)}
        </p>
      )}
    </div>
  );
}
