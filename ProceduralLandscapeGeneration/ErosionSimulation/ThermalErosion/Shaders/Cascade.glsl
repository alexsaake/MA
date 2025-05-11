
//https://github.com/erosiv/soillib/blob/main/source/particle/cascade.hpp
void Cascade(ivec2 ipos)
{
    return;
    if(isOutOfBounds(ipos))
    {
        return;
    }

    // Get Non-Out-of-Bounds Neighbors

    const ivec2 n[] = {
        ivec2(-1, -1),
        ivec2(-1, 0),
        ivec2(-1, 1),
        ivec2(0, -1),
        ivec2(0, 1),
        ivec2(1, -1),
        ivec2(1, 0),
        ivec2(1, 1)
    };

    struct Point {
        ivec2 pos;
        float h;
        float d;
    } sn[8];

    int num = 0;

    for(int i = 0; i < n.length(); i++)
    {
        ivec2 nn = n[i];
        ivec2 npos = ipos + nn;

        if(isOutOfBounds(npos))
        {
            continue;
        }

        float height = heightMap[getIndexV(npos)];
        sn[num].pos = npos;
        sn[num].h = height;
        sn[num].d = length(nn);
        num++;
    }

    // Local Matrix, Target Height

    float height = heightMap[getIndexV(ipos)];
    float h_ave = height;
    for(int i = 0; i < num; ++i)
    {
        h_ave += sn[i].h;
    }
    h_ave /= float(num + 1);

    for (int i = 0; i < num; ++i)
    {
        // Full Height-Different Between Positions!
        float diff = h_ave - sn[i].h;
        if (diff == 0)
        {
            continue;
        }

        ivec2 tpos = (diff > 0) ? ipos : sn[i].pos;
        ivec2 bpos = (diff > 0) ? sn[i].pos : ipos;

        uint tindex = getIndexV(tpos);
        uint bindex = getIndexV(bpos);

        // The Amount of Excess Difference!
        float excess = 0.0f;
        excess = abs(diff) - sn[i].d * particleWindErosionConfiguration.MaxDiff;
        if (excess <= 0)
        {
            continue;
        }

        // Actual Amount Transferred
        float transfer = particleWindErosionConfiguration.Settling * excess / 2.0;
        heightMap[tindex] -= transfer;
        heightMap[bindex] += transfer;
    }
}