import { NavLink } from 'react-router';

// Enlace de la barra lateral o del menú «Más». NavLink marca aria-current="page" en el activo.
export default function EnlaceNavegacion({ seccion, alElegir }) {
  const { a, etiqueta, icono: Icono, fin } = seccion;
  return (
    <NavLink
      to={a}
      end={fin}
      onClick={alElegir}
      className={({ isActive }) =>
        `flex h-10 items-center gap-3 rounded-lg px-3 text-sm font-medium transition-colors ${
          isActive ? 'bg-acento-suave text-acento' : 'text-suave hover:bg-superficie-suave hover:text-texto'
        }`
      }
    >
      <Icono className="size-[18px] shrink-0" aria-hidden="true" />
      {etiqueta}
    </NavLink>
  );
}
