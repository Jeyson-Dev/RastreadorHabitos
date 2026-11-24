// Al montar un formulario, enfoca el campo marcado con data-autofocus (sirve aunque el panel que lo
// contiene se haya abierto antes, por ejemplo mientras cargaban los datos).
export function enfocarInicial(formulario) {
  formulario?.querySelector('[data-autofocus]')?.focus();
}

// Tras un envío con errores, lleva el foco al primer campo marcado como inválido (después de que
// React pinte los errores), para que teclado y lector de pantalla lleguen directo a corregirlo.
export function enfocarPrimerInvalido(formulario) {
  requestAnimationFrame(() => {
    formulario?.querySelector('[aria-invalid="true"]')?.focus();
  });
}
