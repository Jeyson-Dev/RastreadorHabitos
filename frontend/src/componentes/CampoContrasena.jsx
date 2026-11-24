import { useId, useState } from 'react';
import { Check, Circle, Eye, EyeOff } from 'lucide-react';
import { requisitosContrasena } from '../utilidades/contrasena';

// Contraseña con botón para mostrarla u ocultarla y, si se pide, la lista de requisitos del
// servidor que se marcan mientras se escribe.
export default function CampoContrasena({ etiqueta = 'Contraseña', error, conRequisitos = false, accesorio, id, ...entrada }) {
  const generado = useId();
  const idCampo = id ?? generado;
  const idRequisitos = conRequisitos ? `${idCampo}-requisitos` : undefined;
  const idError = error ? `${idCampo}-error` : undefined;
  const [visible, setVisible] = useState(false);
  const requisitos = conRequisitos ? requisitosContrasena(entrada.value ?? '') : [];

  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-baseline justify-between gap-2">
        <label htmlFor={idCampo} className="text-sm font-medium text-texto">
          {etiqueta}
        </label>
        {accesorio}
      </div>
      <div className="relative">
        <input
          {...entrada}
          id={idCampo}
          type={visible ? 'text' : 'password'}
          aria-invalid={error ? true : undefined}
          aria-describedby={[idRequisitos, idError].filter(Boolean).join(' ') || undefined}
          className={`h-10 w-full rounded-lg border bg-superficie pr-11 pl-3 text-sm text-texto focus:border-acento focus:ring-2 focus:ring-acento/30 focus:outline-none ${error ? 'border-peligro' : 'border-borde'}`}
        />
        <button
          type="button"
          onClick={() => setVisible((v) => !v)}
          className="absolute inset-y-0 right-0 inline-flex w-10 items-center justify-center rounded-r-lg text-suave hover:text-texto"
          aria-label={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'}
          aria-pressed={visible}
        >
          {visible ? <EyeOff className="size-4" aria-hidden="true" /> : <Eye className="size-4" aria-hidden="true" />}
        </button>
      </div>
      {conRequisitos && (
        <ul id={idRequisitos} className="flex flex-col gap-1 pt-1" aria-label="Requisitos de la contraseña">
          {requisitos.map((r) => (
            <li key={r.id} className={`flex items-center gap-2 text-xs ${r.cumple ? 'text-acento' : 'text-suave'}`}>
              {r.cumple ? <Check className="size-3.5" aria-hidden="true" /> : <Circle className="size-3.5" aria-hidden="true" />}
              <span>
                {r.texto}
                <span className="sr-only">{r.cumple ? ' (cumplido)' : ' (pendiente)'}</span>
              </span>
            </li>
          ))}
        </ul>
      )}
      {error && (
        <p id={idError} className="text-xs font-medium text-peligro">
          {error}
        </p>
      )}
    </div>
  );
}
