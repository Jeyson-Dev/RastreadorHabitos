import { useId, useRef, useState } from 'react';
import { mensajeDeError } from '../../api/cliente';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Campo from '../../componentes/Campo';
import SelectorDias from '../../componentes/SelectorDias';
import { enfocarPrimerInvalido } from '../../utilidades/formularios';
import { useCrearHabito } from './consultas';

const MAX_NOMBRE = 100;
const MAX_DESCRIPCION = 500;

// Mismas reglas que el servidor: nombre obligatorio (hasta 100), descripción opcional (hasta 500) y
// al menos un día. Un 403 es el límite de hábitos: se muestra el mensaje del servidor.
function validarHabito({ nombre, descripcion, dias }) {
  const errores = {};
  if (!nombre.trim()) errores.nombre = 'Escribe un nombre para el hábito.';
  else if (nombre.trim().length > MAX_NOMBRE) errores.nombre = `El nombre puede tener hasta ${MAX_NOMBRE} caracteres.`;
  if (descripcion.trim().length > MAX_DESCRIPCION) errores.descripcion = `La descripción puede tener hasta ${MAX_DESCRIPCION} caracteres.`;
  if (dias.length === 0) errores.dias = 'Elige al menos un día de la semana.';
  return errores;
}

export default function FormularioHabito({ alCrear }) {
  const crear = useCrearHabito();
  const [nombre, setNombre] = useState('');
  const [descripcion, setDescripcion] = useState('');
  const [dias, setDias] = useState([]);
  const [errores, setErrores] = useState({});
  const formulario = useRef(null);
  const idDescripcion = useId();
  const idErrorDias = useId();

  function alEnviar(evento) {
    evento.preventDefault();
    if (crear.isPending) return;
    const encontrados = validarHabito({ nombre, descripcion, dias });
    setErrores(encontrados);
    if (Object.keys(encontrados).length > 0) {
      enfocarPrimerInvalido(formulario.current);
      return;
    }
    crear.mutate(
      { nombre: nombre.trim(), descripcion: descripcion.trim(), dias },
      { onSuccess: (habito) => alCrear?.(habito) },
    );
  }

  return (
    <form ref={formulario} onSubmit={alEnviar} noValidate className="flex flex-col gap-5">
      {crear.isError && (
        <Aviso tipo="error" titulo={crear.error?.estado === 403 ? 'Llegaste al límite de hábitos' : undefined}>
          {mensajeDeError(crear.error)}
        </Aviso>
      )}
      <Campo
        etiqueta="Nombre"
        placeholder="Leer 20 minutos"
        value={nombre}
        onChange={(e) => setNombre(e.target.value)}
        error={errores.nombre}
        maxLength={MAX_NOMBRE}
        data-autofocus
      />
      <div className="flex flex-col gap-1.5">
        <label htmlFor={idDescripcion} className="text-sm font-medium">
          Descripción <span className="font-normal text-suave">(opcional)</span>
        </label>
        <textarea
          id={idDescripcion}
          rows={3}
          value={descripcion}
          onChange={(e) => setDescripcion(e.target.value)}
          maxLength={MAX_DESCRIPCION}
          aria-invalid={errores.descripcion ? true : undefined}
          placeholder="Antes de dormir"
          className="rounded-lg border border-borde bg-superficie px-3 py-2 text-sm focus:border-acento focus:ring-2 focus:ring-acento/30 focus:outline-none"
        />
        {errores.descripcion && <p className="text-xs font-medium text-peligro">{errores.descripcion}</p>}
      </div>
      <SelectorDias valor={dias} alCambiar={setDias} error={errores.dias} idError={idErrorDias} />
      <Boton type="submit" cargando={crear.isPending} tamano="lg" className="w-full">
        Crear hábito
      </Boton>
    </form>
  );
}
