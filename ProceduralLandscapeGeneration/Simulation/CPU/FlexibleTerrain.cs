namespace ProceduralLandscapeGeneration.Simulation.CPU;

internal class FlexibleTerrain
{
    //TODO Velocity Verlet integral/Leap frog x Ort, v Geschwindigkeit

    //Stookesche Gesetzt für Dämpfung

    void ParticleDynamics()
    {
        //const float dt; //Zeitschritt
        //var radius;
        //const float rohf = ; //Konstante
        //var g = 9.81f;
        //var mass;
        //var forceExtern = mass * g - Volume(radius) * rohf;

        //var v0;
        //var settling = 2/g * g * MathF.Pow(, 2);
        //var v1 = forceExtern * dt + settling * v0;

        //Mp, R, rho_f gegeben
        //V = 4 / 3 * PI * R ^ 3
        //Fext = M * g - 4 / 3 * PI * R ^ 3 * rho_f
        //v(t1) = v(t0) + F_ext * dt + ws
        //a(t) = F_ext / M
        //a(t) = g - 4/3 * PI * R^3 * rho_f / M
        //Beschleunigungsvektor a(t) = (0, 0, 4/3 * PI * R^3 * rho_f / M - g)


    }

    private float Volume(float radius)
    {
        return 4 / 3 * MathF.PI * MathF.Pow(radius, 2);
    }
}
