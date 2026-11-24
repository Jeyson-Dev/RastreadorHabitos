// Tras un envío con errores, lleva el foco al primer campo marcado como inválido (después de que
// React pinte los errores), para que teclado y lector de pantalla lleguen directo a corregirlo.
export function enfocarPrimerInvalido(formulario) {
  requestAnimationFrame(() => {
    formulario?.querySelector('[aria-invalid="true"]')?.focus();
  });
}
