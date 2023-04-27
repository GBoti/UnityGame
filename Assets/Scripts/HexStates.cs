using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum rule{
    water,
    ground
}

public enum side{
    left,
    topleft,
    topright,
    right,
    bottomright,
    bottomleft
}

public struct State{
    public string name;
    public rule[] rules;

    public State(string n, rule[] r){
        name = n;
        rules = r;
    }
}

public class HexStates{
    private List<State> states = new List<State> {
        new State("water", new rule[6] {rule.water,rule.water,rule.water,rule.water,rule.water,rule.water}),
        new State("ground", new rule[6] {rule.ground,rule.ground,rule.ground,rule.ground,rule.ground,rule.ground}),
        //one water
        new State("ground_w_l", new rule[6] {rule.water,rule.ground,rule.ground,rule.ground,rule.ground,rule.ground}),
        new State("ground_w_tl", new rule[6] {rule.ground,rule.water,rule.ground,rule.ground,rule.ground,rule.ground}),
        new State("ground_w_tr", new rule[6] {rule.ground,rule.ground,rule.water,rule.ground,rule.ground,rule.ground}),
        new State("ground_w_r", new rule[6] {rule.ground,rule.ground,rule.ground,rule.water,rule.ground,rule.ground}),
        new State("ground_w_br", new rule[6] {rule.ground,rule.ground,rule.ground,rule.ground,rule.water,rule.ground}),
        new State("ground_w_bl", new rule[6] {rule.ground,rule.ground,rule.ground,rule.ground,rule.ground,rule.water}),
        //two water
        new State("ground_w_l_tl", new rule[6] {rule.water,rule.water,rule.ground,rule.ground,rule.ground,rule.ground}),
        new State("ground_w_l_tr", new rule[6] {rule.water,rule.ground,rule.water,rule.ground,rule.ground,rule.ground}),
        new State("ground_w_l_r", new rule[6] {rule.water,rule.ground,rule.ground,rule.water,rule.ground,rule.ground}),
        new State("ground_w_l_br", new rule[6] {rule.water,rule.ground,rule.ground,rule.ground,rule.water,rule.ground}),
        new State("ground_w_l_bl", new rule[6] {rule.water,rule.ground,rule.ground,rule.ground,rule.ground,rule.water}),

        new State("ground_w_tl_tr", new rule[6] {rule.ground,rule.water,rule.water,rule.ground,rule.ground,rule.ground}),
        new State("ground_w_tl_r", new rule[6] {rule.ground,rule.water,rule.ground,rule.water,rule.ground,rule.ground}),
        new State("ground_w_tl_br", new rule[6] {rule.ground,rule.water,rule.ground,rule.ground,rule.water,rule.ground}),
        new State("ground_w_tl_bl", new rule[6] {rule.ground,rule.water,rule.ground,rule.ground,rule.ground,rule.water}),

        new State("ground_w_tr_r", new rule[6] {rule.ground,rule.ground,rule.water,rule.water,rule.ground,rule.ground}),
        new State("ground_w_tr_br", new rule[6] {rule.ground,rule.ground,rule.water,rule.ground,rule.water,rule.ground}),
        new State("ground_w_tr_bl", new rule[6] {rule.ground,rule.ground,rule.water,rule.ground,rule.ground,rule.water}),

        new State("ground_w_r_br", new rule[6] {rule.ground,rule.ground,rule.ground,rule.water,rule.water,rule.ground}),
        new State("ground_w_r_bl", new rule[6] {rule.ground,rule.ground,rule.ground,rule.water,rule.ground,rule.water}),

        new State("ground_w_br_bl", new rule[6] {rule.ground,rule.ground,rule.ground,rule.ground,rule.water,rule.water}),
        //three water
        new State("ground_w_l_tl_tr", new rule[6] {rule.water,rule.water,rule.water,rule.ground,rule.ground,rule.ground}),
        new State("ground_w_l_tl_r", new rule[6] {rule.water,rule.water,rule.ground,rule.water,rule.ground,rule.ground}),
        new State("ground_w_l_tl_br", new rule[6] {rule.water,rule.water,rule.ground,rule.ground,rule.water,rule.ground}),
        new State("ground_w_l_tl_bl", new rule[6] {rule.water,rule.water,rule.ground,rule.ground,rule.ground,rule.water}),

        new State("ground_w_l_tr_r", new rule[6] {rule.water,rule.ground,rule.water,rule.water,rule.ground,rule.ground}),
        new State("ground_w_l_tr_br", new rule[6] {rule.water,rule.ground,rule.water,rule.ground,rule.water,rule.ground}),
        new State("ground_w_l_tr_bl", new rule[6] {rule.water,rule.ground,rule.water,rule.ground,rule.ground,rule.water}),

        new State("ground_w_l_r_br", new rule[6] {rule.water,rule.ground,rule.ground,rule.water,rule.water,rule.ground}),
        new State("ground_w_l_r_bl", new rule[6] {rule.water,rule.ground,rule.ground,rule.water,rule.ground,rule.water}),

        new State("ground_w_l_br_bl", new rule[6] {rule.water,rule.ground,rule.ground,rule.ground,rule.water,rule.water}),

        new State("ground_w_tl_tr_r", new rule[6] {rule.ground,rule.water,rule.water,rule.water,rule.ground,rule.ground}),
        new State("ground_w_tl_tr_br", new rule[6] {rule.ground,rule.water,rule.water,rule.ground,rule.water,rule.ground}),
        new State("ground_w_tl_tr_bl", new rule[6] {rule.ground,rule.water,rule.water,rule.ground,rule.ground,rule.water}),

        new State("ground_w_tl_r_br", new rule[6] {rule.ground,rule.water,rule.ground,rule.water,rule.water,rule.ground}),
        new State("ground_w_tl_r_bl", new rule[6] {rule.ground,rule.water,rule.ground,rule.water,rule.ground,rule.water}),

        new State("ground_w_tl_br_bl", new rule[6] {rule.ground,rule.water,rule.ground,rule.ground,rule.water,rule.water}),

        new State("ground_w_tr_r_br", new rule[6] {rule.ground,rule.ground,rule.water,rule.water,rule.water,rule.ground}),
        new State("ground_w_tr_r_bl", new rule[6] {rule.ground,rule.ground,rule.water,rule.water,rule.ground,rule.water}),

        new State("ground_w_tr_br_bl", new rule[6] {rule.ground,rule.ground,rule.water,rule.ground,rule.water,rule.water}),

        new State("ground_w_r_br_bl", new rule[6] {rule.ground,rule.ground,rule.ground,rule.water,rule.water,rule.water}),
        //four water
        new State("ground_w_l_tl_tr_r", new rule[6] {rule.water,rule.water,rule.water,rule.water,rule.ground,rule.ground}),
        new State("ground_w_l_tl_tr_br", new rule[6] {rule.water,rule.water,rule.water,rule.ground,rule.water,rule.ground}),
        new State("ground_w_l_tl_tr_bl", new rule[6] {rule.water,rule.water,rule.water,rule.ground,rule.ground,rule.water}),

        new State("ground_w_l_tl_r_br", new rule[6] {rule.water,rule.water,rule.ground,rule.water,rule.water,rule.ground}),
        new State("ground_w_l_tl_r_bl", new rule[6] {rule.water,rule.water,rule.ground,rule.water,rule.ground,rule.water}),

        new State("ground_w_l_tl_br_bl", new rule[6] {rule.water,rule.water,rule.ground,rule.ground,rule.water,rule.water}),

        new State("ground_w_l_tr_r_br", new rule[6] {rule.water,rule.ground,rule.water,rule.water,rule.water,rule.ground}),
        new State("ground_w_l_tr_r_bl", new rule[6] {rule.water,rule.ground,rule.water,rule.water,rule.ground,rule.water}),

        new State("ground_w_l_tr_br_bl", new rule[6] {rule.water,rule.ground,rule.water,rule.ground,rule.water,rule.water}),

        new State("ground_w_l_r_br_bl", new rule[6] {rule.water,rule.ground,rule.ground,rule.water,rule.water,rule.water}),

        new State("ground_w_tl_tr_r_br", new rule[6] {rule.ground,rule.water,rule.water,rule.water,rule.water,rule.ground}),
        new State("ground_w_tl_tr_r_bl", new rule[6] {rule.ground,rule.water,rule.water,rule.water,rule.ground,rule.water}),

        new State("ground_w_tl_tr_br_bl", new rule[6] {rule.ground,rule.water,rule.water,rule.ground,rule.water,rule.water}),

        new State("ground_w_tl_r_br_bl", new rule[6] {rule.ground,rule.water,rule.ground,rule.water,rule.water,rule.water}),

        new State("ground_w_tr_r_br_bl", new rule[6] {rule.ground,rule.ground,rule.water,rule.water,rule.water,rule.water}),
        //five water
        new State("ground_w_l_tl_tr_r_br", new rule[6] {rule.water,rule.ground,rule.ground,rule.water,rule.water,rule.water}),
        new State("ground_w_l_tl_tr_r_bl", new rule[6] {rule.water,rule.ground,rule.ground,rule.water,rule.water,rule.water}),

        new State("ground_w_l_tl_tr_br_bl", new rule[6] {rule.water,rule.ground,rule.ground,rule.water,rule.water,rule.water}),

        new State("ground_w_l_tl_r_br_bl", new rule[6] {rule.water,rule.ground,rule.ground,rule.water,rule.water,rule.water}),

        new State("ground_w_l_tr_r_br_bl", new rule[6] {rule.water,rule.ground,rule.ground,rule.water,rule.water,rule.water}),

        new State("ground_w_tl_tr_r_br_bl", new rule[6] {rule.water,rule.ground,rule.ground,rule.water,rule.water,rule.water})
        //if there is six water around it is a water tile
    };

    public List<State> States{
        get => states;
    }

    public int Count{
        get => states.Count;
    }

    public void Collapse(){
        State rndState = states[UnityEngine.Random.Range(0, states.Count - 1)];
        states = new List<State>(){rndState};
    }

    public void Reduce(side s, rule r){ //r is the middle of the cell
        List<State> itemsToRemove = states.FindAll(state => state.rules[(int)s] == r);
        foreach(State itr in itemsToRemove){
            states.Remove(itr);
        }
    }
}
