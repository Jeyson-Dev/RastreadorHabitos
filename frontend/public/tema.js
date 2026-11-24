// Aplica el tema guardado (o el del sistema) antes de que se pinte la página, para que no parpadee.
(function () {
  function leerGuardado() {
    try {
      return localStorage.getItem('rh.tema');
    } catch {
      return null;
    }
  }
  var guardado = leerGuardado();
  var oscuro =
    guardado === 'oscuro' ||
    ((guardado === null || guardado === 'sistema') && window.matchMedia('(prefers-color-scheme: dark)').matches);
  document.documentElement.classList.toggle('dark', oscuro);
})();
