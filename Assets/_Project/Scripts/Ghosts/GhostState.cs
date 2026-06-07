public enum GhostState
{
    InHouse,    // Dentro de la ghost house, esperando salir
    Leaving,    // Saliendo de la ghost house hacia el exit point
    Scatter,    // Hacia su esquina asignada
    Chase,      // Persiguiendo según su lógica personal
    Frightened, // Asustado (Pac-Man comió power pellet)
    Eaten       // Comido por Pac-Man, regresando a la ghost house

}
