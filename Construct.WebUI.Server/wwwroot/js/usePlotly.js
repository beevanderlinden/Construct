
// global 
{
    var ForceScale = 0.2;
    var MomentScale = 0.1;
    var ShearScale = 0.1;
    var AxialForceScale = 0.1;

    var MomentColor = 'red';
    var ShearColor = 'blue';
    var DisplacementColor = 'black';
    var AxialForceColor = 'green';

    var MomentExtremeAbsolute = 0;
    var MemberIndex = 0;
    var MembersLastIndex = 0;




    var myfont = { family: 'Arial, sans-serif', size: 12, color: 'black' };

    // nodes
    var nodeColor = 'brown';
    var nodeFont = { family: 'Arial, sans-serif', size: 10, color: nodeColor };
    var nodeSize = 10;

    // members
    var memberColor = 'brown';
    var memberFont = { family: 'Arial, sans-serif', size: 10, color: memberColor };
    var memberWidth = 3;

    // pin
    var pinColor = 'white';
    var pinLine = { color: 'black', width: 1 };


    var pinSize = 15;


    //var mat2, vec2 = glMatrix;
}


window.updatePlotlyRekenspion = (plotlyId, geometryService, memberActionsList, numberOfRows, numberOfColumns) => {

    // validate input
    if (memberActionsList.length == 0) {
        return;
    }

    // per node (x, z, rad) 
    var verplaatsingen = geometryService.results.globalDisplacementPoints;
    var memberDisplacements = geometryService.results.globalDisplacementPointsByMember;

    

    //let results = memberDisplacements.find((item) => {
    //    return item.memberId == memberActionsList[0].member.id;
    //});


    // verwijs naar div
    const chartDiv = document.getElementById(plotlyId);

    // teken de grafieken
    // V, M, N, u


    let xValues = [];
    let yValuesM = [];
    let yValuesV = [];
    let yValuesN = [];
    //let xValuesDisplacement = [];
    let yValuesDisplacement = [];

    let textM = [];
    let textV = [];
    let textN = [];
    let textD = [];
    
    //let member0 = results.memberActionsList[0];



    let x = 0;
    let aantalActions = memberActionsList.length;

    let actions = memberActionsList;

    //// a comes after b
    //// welk quadrant? (1)

    //actions.sort((a, b) => (a.member.startNode.x + a.member.endNode.x) - (b.member.startNode.x + b.member.endNode.x));



    // todo geschikt maken voor omgekeerde staven.
    // startnode ligt niet altijd links, dus maak een lijst voor ALLE nodes
    // sorteer op X en dan doorgaan
    for (let i = 0; i < aantalActions; i++)
    {
        xValues.push(x, x + actions[i].member.length);

        yValuesM.push(-actions[i].momentStartNode.toFixed(3), actions[i].momentEndNode.toFixed(3));
        textM.push(-actions[i].momentStartNode.toFixed(1),  actions[i].momentEndNode.toFixed(1));

        yValuesV.push(-actions[i].shearStartNode.toFixed(3), actions[i].shearEndNode.toFixed(3));
        textV.push(-actions[i].shearStartNode.toFixed(1), actions[i].shearEndNode.toFixed(1));

        yValuesN.push(actions[i].axialForce.toFixed(3), actions[i].axialForce.toFixed(3));
        textN.push(actions[i].axialForce.toFixed(1), actions[i].axialForce.toFixed(1));

        let results = memberDisplacements[actions[i].member.id - 1];
        //xValuesDisplacement.push(x, x + actions[i].member.length);
        yValuesDisplacement.push(results[0].globalDisplacement.z * 1000, results[1].globalDisplacement.z * 1000);
        textD.push((results[0].globalDisplacement.z * 1000).toFixed(1), (results[1].globalDisplacement.z * 1000).toFixed(1));

        x += actions[i].member.length;
    }

    
    var trace1 = {
        x: xValues,
        y: yValuesM,

        type: 'scatter',
        mode: 'lines+text',
        line: {
            color: MomentColor,
            width: 1
        },
        name: 'M-lijn',
        fill: 'tozeroy',
        


        text: textM,
        textposition: ['right', 'left'],
        textfont: myfont,
    };

    var trace2 = {
        x: xValues,
        y: yValuesV,
        xaxis: 'x2',
        yaxis: 'y2',
        type: 'scatter',
        mode: 'lines+text',
        line: {
            color: ShearColor,
            width: 1
        },
        name: 'V-lijn',
        fill: 'tozeroy',
       
        text: textV,
        textposition: ['right', 'left'],
        textfont:myfont,
    };

    var trace3 = {
        x: xValues,
        y: yValuesDisplacement,
        xaxis: 'x3',
        yaxis: 'y3',
        type: 'scatter',
        mode: 'lines+text',
        line: {
            color: DisplacementColor,
            width: 1
        },
        name: 'Uz',

        text: textD,
        textposition: ['right', 'left'],
        textfont: myfont,
    };

    var trace4 = {
        x: xValues,
        y: yValuesN,
        xaxis: 'x4',
        yaxis: 'y4',
        
        type: 'scatter',
        mode: 'lines+text',
        line: {
            color: AxialForceColor,
            width: 1
        },
        name: 'N-lijn',
        fill: 'tozeroy',

        text: textN,
        textposition: ['right', 'left' ],
        textfont: myfont,

    };

    var data = [trace1, trace2, trace3, trace4];

    var layout = {
        autosize: true,
        grid:   { rows: numberOfRows, columns: numberOfColumns, pattern: 'independent' },

        xaxis:  { visible: false, fixedrange: true, },
        yaxis:  { visible: true, rangemode: 'tozero', fixedrange: true, title: { text: 'M [kNm]', font: myfont } },
        
        xaxis2: { visible: false, fixedrange: true, },
        yaxis2: { visible: true, fixedrange: true, title: {text: 'V [kN]', font: myfont}},

        xaxis3: { visible: false, fixedrange: true, },
        yaxis3: { visible: true, rangemode: 'tozero', fixedrange: true, title: { text: 'verpl. [mm]', font: myfont } },


        xaxis4: { visible: false, fixedrange: true,  },
        yaxis4: { visible: true, fixedrange: true, title: { text: 'N [kN]', font: myfont } },


        pad: { l: 20, r: 20, t: 20, b: 20 },
        
        

    };

    Plotly.newPlot(chartDiv, data, layout);

}

window.updatePlotlyChart = (plotlyId, geometry, mesh, results, display) => {
    const chartDiv = document.getElementById(plotlyId);
    if (!chartDiv) {
        console.error('Plotly chart element not found');
        return;
    }

    let data = [];
    let shapes = [];

    let model = geometry;
    if (display.showMesh && mesh.members.length > 0) {
        model = mesh;
    }

    // altijd

    let memberShapes = getMemberShapes(model, display);
    shapes.push(...memberShapes)

   

    if (display.showGeometry) {
        data.push(getNodesWithText(model));

        data.push(getStartNodePins(model));

        // shapes
        let arrayNodalForces = createNodalForces(geometry.nodalForces);



        let memberForcesShapes = getLijnlasten(geometry, geometry.memberForces);
       

        let springShapes = geometry.springs.map(createSpringShape);
        let supportShapes = geometry.supports.map(createSupportShape);

        shapes.push(...arrayNodalForces, ...memberForcesShapes, ...springShapes, ...supportShapes);
    }
    

    if (display.showDisplacement) {
        const displacementItems = getVerplaatsingPerMember(results.globalDisplacementPointsByMember);
        data.push(...displacementItems);
    }

    if (display.showMoment) {
        const momentItems = getMomentPerMember(results.memberActionsList);
        data.push(...momentItems);
    }

    if (display.showShear) {
        const shearItems = getShearPerMember(results.memberActionsList);
        data.push(...shearItems);
    }

    if (display.showAxialForce) {
        const axialForceItems = getAxialForceByMember(results.memberActionsList);
        data.push(...axialForceItems);
    }

        
    data.push(getReacties(results.reactions));
    

    let layout = getLayout(geometry, shapes, display);
    var config = { responsive: true };

    // Render Plotly chart
    Plotly.newPlot(chartDiv, data, layout, config)
        .catch(error => console.error('Plotly.newPlot failed:', error));
};




function createNodalForces(nodalForces) {

    // set de scale
    // todo filter out momentload
    const absMagnitudes = nodalForces.map(f => Math.abs(f.magnitude));
    ForceScale = 1.0 / Math.max(...absMagnitudes);

    const newArray = nodalForces.map(createNodalForce);

    return newArray;
}


function createSupportShape(support) {
    const supportPath = getSupportPath(support);
    const shape = {
        type: 'path',
        path: supportPath,
        line: {
            color: 'rgb(44, 160, 101)'
        },
    }
    return shape;
}

function createSpringShape(spring) {
    const springPath = getSpringPath(spring);
    const shape = {
        type: 'path',
        path: springPath,
        line: {
            color: 'rgb(44, 160, 101)'
        },
    }
    return shape;
}

function createPointload(memberForce) {
    let pointA = memberForce.pointA;

}


function createNodalForce(nodalForce) {
    const x = nodalForce.node.x;
    const y = nodalForce.node.z; 

    // todo minimum length!
    const length = nodalForce.magnitude * ForceScale;
    const tip = length * 0.1;


    let dx = 0;
    let dy = length;
    let path = '';

    let txt = String(Math.abs(nodalForce.magnitude));
    let txtPos = 'top center'

    switch (nodalForce.type) {
        case 2: // PZ
            dx = 0;
            dy = length;
            path = `
            M${x - dx},${y - dy} 
            L${x},${y} 
            L${x - tip/2},${y - tip} 
            L${x + tip/2},${y - tip} 
            L${x},${y}`;

            txt += " kN";

            if (length > 0) { txtPos = 'bottom left' } else { txtPos = 'top left' }

            


            break;
        case 1: // PX
            dx = length;
            dy = 0;
            path = `
            M${x - dx},${y - dy} 
            L${x},${y} 
            L${x - tip},${y - tip/2} 
            L${x - tip},${y + tip/2} 
            L${x},${y}`;

            txt += " kN";

            if (length > 0) { txtPos = 'inside' } else {txtPos = 'inside' }

            break;
        case 3: //MY
            const rx = 1;
            const ry = 1;
            
            //path = `M${x},${y} A${rx},${ry},0,0,0, ${x},${y} L${x},${y}`;
            path = `M${x},${y} Q${x+rx},${y} ${x},${y-ry} ${x-rx},${y}`;


            txt += " kNm";
           


            break;


    }




    const shape = {
        type: 'path',
        path: path,
        fillcolor: 'rgba(44, 160, 101, 0.5)',
        line: {
            color: 'rgb(44, 160, 101)'
        },
        label: {
            text: txt,
            font: { size: 10, color: 'black' },
            textposition: txtPos,
            //textangle: 90,
        },
    }


    return shape;


}





function getLayout(model, shapes, display) {



    const layout = {
        title: display.title,
        shapes: shapes,
        xaxis: {
            //title: 'X',
            //range: [xMin - 1.5 * sizeFactor, xMax + sizeFactor],
            automargin: true,
            showgrid: true, // thin lines
            showline: false,
            zeroline: false, // 
            visible: true, // number below
            ticks: 'outside',
            //nticks: 5,
            tickvals: model.nodes.map(item => item.x),
            tickformat: '.3f',
            //tick0: 0,
            //dtick: 0.5,
            ticklen: 10,
            tickcolor: 'red',
            //
            //domain: [0, 1],
            //rangemode: 'nonnegative',
            autorange: true,
        },
        yaxis: {
            //title: 'Y',
            //range: [yMin - 1.5 * sizeFactor, yMax + sizeFactor],
            automargin: true,
            scaleanchor: 'x',
            scaleratio: 1,
            showgrid: true,
            showline: false,
            zeroline: false,
            visible: true,
            ticks: 'outside',
            ticklen: 20,
            tickcolor: 'crimson',
            tickvals: model.nodes.map(item => item.z),
            tickformat: '.3f',
            //tick0: 0,
            //dtick: 0.5,
            //nticks: 5,
            //domain: [0, 1],
            //rangemode: 'nonnegative',
            //autorange: true,
        },

        margin: { t: 50, b: 50, l: 50, r: 50 },
        autosize: true,
        showlegend: false,
        plot_bgcolor: "#FFF3",
        paper_bgcolor: "#FFF3"
       



    };
    return layout;
}




function getVerplaatsingPerMember(globalDisplacementPointsPerMember)
{
    const array = globalDisplacementPointsPerMember.map(getVerplaatsing);
    return array;
}


function getShearPerMember(memberActionsList) {
    const valuesStart = memberActionsList.map(item => Math.abs(item.shearStartNode));
    const valuesEnd = memberActionsList.map(item => Math.abs(item.shearEndNode));
    const values = valuesStart.concat(valuesEnd);
    const absGrootsteWaarde = Math.max(...values);
    if (absGrootsteWaarde > 0) {
        ShearScale = 1.0 / absGrootsteWaarde;
    }


    const array = memberActionsList.map(getShear);
    return array;
}

function getAxialForceByMember(memberActionsList) {
    // set scale
    const values = memberActionsList.map(item => Math.abs(item.axialForce));
    const absMax = Math.max(...values);
    if (absMax > 0) {
        AxialForceScale = 1.0 / absMax;
    }

    const array = memberActionsList.map(getAxialForce);
    return array;

}


function getMomentPerMember(memberActionsList) {
    // stel de schaal in
    // 1 meter voor abs-grootste
    const m1s = memberActionsList.map(item => Math.abs(item.momentStartNode));
    const m2s = memberActionsList.map(item => Math.abs(item.momentEndNode));
    const momenten = m1s.concat(m2s);
    const absGrootsteWaarde = Math.max(...momenten);

    if (absGrootsteWaarde > 0) {
        MomentScale = 1.0 / absGrootsteWaarde;
        MomentExtremeAbsolute = absGrootsteWaarde.toFixed(1);
    }

    MemberIndex = 0; // this variable used in function 'getMoment' that maps single item
    MembersLastIndex = memberActionsList.length - 1;
   

    const array = memberActionsList.map(getMoment);
    return array;
}

function getAxialForce(memberActions) {
    const { vec2 } = glMatrix;
    let g1 = vec2.fromValues(memberActions.member.startNode.x, memberActions.member.startNode.z);
    let g2 = vec2.fromValues(memberActions.member.endNode.x, memberActions.member.endNode.z);
    const direction = vec2.create();
    vec2.subtract(direction, g2, g1);
    let thetaX = Math.atan2(direction[1], direction[0]);
    let thetaZ = thetaX + Math.PI / 2;
    let val1 = vec2.fromValues(
        g1[0] + memberActions.axialForce * AxialForceScale * Math.cos(thetaZ),
        g1[1] + memberActions.axialForce * AxialForceScale * Math.sin(thetaZ));

    let val2 = vec2.fromValues(
        g2[0] + memberActions.axialForce * AxialForceScale * Math.cos(thetaZ),
        g2[1] + memberActions.axialForce * AxialForceScale * Math.sin(thetaZ));

    let points = [g1, val1, val2, g2];
    let txtAxialForce = ['', '', memberActions.axialForce.toFixed(1), ''];
    const trace = {
        x: points.map(item => item[0]),
        y: points.map(item => item[1]),
        type: 'scatter',
        mode: 'lines+text',
        name: 'axialForce',
        line: {
            color: AxialForceColor,
            width: 1
        },
        fill: 'toself',
        text: txtAxialForce,
        textposition: 'bottom',
        textfont: myfont,
        hoverinfo: 'skip', // niet tonen, is verschaalde waarde!
    };
    return trace;
}


function getShear(memberActions) {
    const { vec2 } = glMatrix;
    let g1 = vec2.fromValues(memberActions.member.startNode.x, memberActions.member.startNode.z);
    let g2 = vec2.fromValues(memberActions.member.endNode.x, memberActions.member.endNode.z);
    // values in direction of local-z axis
    const direction = vec2.create();
    vec2.subtract(direction, g2, g1);
    // Calculate the angle using atan2
    let thetaX = Math.atan2(direction[1], direction[0]);
    let thetaZ = thetaX + Math.PI / 2;

    let val1 = vec2.fromValues(
        g1[0] + -memberActions.shearStartNode * ShearScale * Math.cos(thetaZ),
        g1[1] + -memberActions.shearStartNode * ShearScale * Math.sin(thetaZ));

    let val2 = vec2.fromValues(
        g2[0] + memberActions.shearEndNode * ShearScale * Math.cos(thetaZ),
        g2[1] + memberActions.shearEndNode * ShearScale * Math.sin(thetaZ));

    let points = [g1, val1, val2, g2];

    let txtShear = ['', '', memberActions.shearEndNode.toFixed(1), ''];
     

    const trace = {
        x: points.map(item => item[0]),
        y: points.map(item => item[1]),
        type: 'scatter',
        mode: 'lines+text',
        name: 'shear',
        line: {
            color: ShearColor,
            width: 1
        },
        fill: 'toself',
        text: txtShear,
        textposition: 'bottom',
        textfont: myfont,
        hoverinfo: 'skip', // niet tonen, is verschaalde waarde!
    };
    return trace;
}

function getMoment(memberActions) {
    const { vec2 } = glMatrix;
    let g1 = vec2.fromValues(memberActions.member.startNode.x, memberActions.member.startNode.z);
    let g2 = vec2.fromValues(memberActions.member.endNode.x, memberActions.member.endNode.z);

    // we tekenen het moment ten opzicht van de locale z-as
    const direction = vec2.create();
    vec2.subtract(direction, g2, g1);
    // Calculate the angle using atan2
    let thetaX = Math.atan2(direction[1], direction[0]);
    let thetaZ = thetaX + Math.PI / 2;

    let m1 = vec2.fromValues(
        g1[0] + -memberActions.momentStartNode * MomentScale * Math.cos(thetaZ),
        g1[1] + -memberActions.momentStartNode * MomentScale * Math.sin(thetaZ));

    let m2 = vec2.fromValues(
        g2[0] + memberActions.momentEndNode * MomentScale * Math.cos(thetaZ),
        g2[1] + memberActions.momentEndNode * MomentScale * Math.sin(thetaZ));

    let points = [g1, m1, m2, g2];

    // sort out text, only show what we want to see
    let mStart = -memberActions.momentStartNode.toFixed(1);
    let mEnd = memberActions.momentEndNode.toFixed(1);
    
    let txt1 = '';
    if (mStart != 0.0 && Math.abs(mStart) == MomentExtremeAbsolute) {
        txt1 = mStart;
    }


    let txt2 = '';
    if (
        mEnd != 0.0 &&
        Math.abs(mEnd) == MomentExtremeAbsolute &&
        MemberIndex++ == MembersLastIndex
    ) {
        txt2 = mEnd;
    }

    let txtMoment = ['', txt1, txt2, ''];




    const trace = {
        x: points.map(item => item[0]),
        y: points.map(item => item[1]),
        type: 'scatter',
        mode: 'lines+text',
        name: 'moment',
        line: {
            color: MomentColor,
            width: 1
        },
        fill: 'toself',
        text: txtMoment,
        textposition: 'bottom',
        textfont: myfont,
        hoverinfo: 'skip', // niet tonen, is verschaalde waarde!
    };
    return trace;


}




function getVerplaatsing(globalDisplacementPoints) {
    const trace = {
        x: globalDisplacementPoints.map(item => item.p.x),
        y: globalDisplacementPoints.map(item => item.p.z),
        type: 'scatter',
        mode: 'lines+text',
        name: 'verplaatsing',
        line: {
            color: 'rgb(128, 0, 128)',
            width: 1
        },
        text: globalDisplacementPoints.map(item => (item.globalDisplacement.z * 1000).toFixed(2) === "0.00" ? "": (item.globalDisplacement.z * 1000).toFixed(2)),
        textposition: 'bottom',
        textfont: myfont,
        hoverinfo: 'skip', // niet tonen, is verschaalde waarde!
    };
    return trace;
}

function getReacties(reacties) {
    const trace = {
        x: reacties.map(item => item.pos.x),
        y: reacties.map(item => item.pos.z),
        type: 'scatter',
        mode: 'markers+text',
        text: reacties.map(item => item.userFriendlyName),
        textposition: 'bottom',
    }
    return trace;
}



function getNodesWithText(model) {
    const trace = {
        mode: 'markers+text',
        type: 'scatter',
        x: model.nodes.map(item => item.x),
        y: model.nodes.map(item => item.z),
        text: model.nodes.map(item => item.id),
        textposition: 'bottom right',
        textfont: nodeFont,

        marker: { size: nodeSize, color: nodeColor },
        hoverinfo: 'skip',
       
    }
    return trace;
}


function checkStartNodePin(member) {
    return member.pinStartNode;
}

function getStartNodePins(model) {

    const membersWithPinStartNode = model.members.filter(checkStartNodePin);


    const trace = {
        mode: 'markers',
        type: 'scatter',
        x: membersWithPinStartNode.map(item => item.startNode.x),
        y: membersWithPinStartNode.map(item => item.startNode.z),
        marker: {
            size: pinSize, color: pinColor, line: pinLine },
        hoverinfo: 'skip',
    }
    return trace;
}



function getNodes3d(model) {
    const trace = {
        mode: 'markers',
        type: 'scatter3d',
        x: model.nodes.map(item => item.x),
        y: model.nodes.map(item => item.y),
        z: model.nodes.map(item => item.z),
        marker: {size: 10},

    }
    return trace;
}

function getMembers(model, color, width) {
    const membersTraces = model.members.map(member => ({
        x: [member.startNode.x, member.endNode.x],
        y: [member.startNode.z, member.endNode.z],
        mode: 'lines',
        line: { color: color, width: width },
        type: 'scatter'
    }));
    return membersTraces;
}


function getSupportPath(support) {
    // get shape respresenting a support
    const { mat2, vec2 } = glMatrix;
    let size = 0.1;
    let path = '';
    const start = vec2.fromValues(support.node.x, support.node.z);
    let theta = 0;
    // Create a 2D rotation matrix
    const rotationMatrix = mat2.fromRotation(mat2.create(), theta);

    let pointsOriginal = [];
    let pointsRotated = [];
    let pointsFinal = [];

    if (support.ry) {
        // rechthoek voor inklemming
        pointsOriginal = [
            vec2.fromValues(-size, size),
            vec2.fromValues(size, size),
            vec2.fromValues(size, -size),
            vec2.fromValues(-size, -size)];
        pointsRotated = [vec2.create(), vec2.create(), vec2.create(), vec2.create()];
        pointsFinal = [vec2.create(), vec2.create(), vec2.create(), vec2.create()];

        // Apply the rotation matrix to the point
        for (let i = 0; i < pointsOriginal.length; i++) {
            vec2.transformMat2(pointsRotated[i], pointsOriginal[i], rotationMatrix);
            pointsFinal[i] = vec2.fromValues(start[0] + pointsRotated[i][0], start[1] + pointsRotated[i][1]);
        }

        path += `M${pointsFinal[0][0]},${pointsFinal[0][1]}`;
        path += ` L${pointsFinal[1][0]},${pointsFinal[1][1]}`;
        path += ` L${pointsFinal[2][0]},${pointsFinal[2][1]}`;
        path += ` L${pointsFinal[3][0]},${pointsFinal[3][1]}`;
        path += ` L${pointsFinal[0][0]},${pointsFinal[0][1]}`;
    }
    if (support.uz) {
        // driehoek voor vast in Z-richting
        pointsOriginal = [
            vec2.fromValues(0, -size),
            vec2.fromValues(0, -size * 1.5),
            vec2.fromValues(-size, -size * 2.5),
            vec2.fromValues(size, -size * 2.5)];
        pointsRotated = [vec2.create(), vec2.create(), vec2.create(), vec2.create()];
        pointsFinal = [vec2.create(), vec2.create(), vec2.create(), vec2.create()];

        // Apply the rotation matrix to the point
        for (let i = 0; i < pointsOriginal.length; i++) {
            vec2.transformMat2(pointsRotated[i], pointsOriginal[i], rotationMatrix);
            pointsFinal[i] = vec2.fromValues(start[0] + pointsRotated[i][0], start[1] + pointsRotated[i][1]);
        }

        path += `M${pointsFinal[0][0]},${pointsFinal[0][1]}`;
        path += ` L${pointsFinal[1][0]},${pointsFinal[1][1]}`;
        path += ` L${pointsFinal[2][0]},${pointsFinal[2][1]}`;
        path += ` L${pointsFinal[3][0]},${pointsFinal[3][1]}`;
        path += ` L${pointsFinal[1][0]},${pointsFinal[1][1]}`;

    }
    if (!support.ux) {
        // streep voor LOS in X-richting
        pointsOriginal = [
            vec2.fromValues(-size, -size * 3),
            vec2.fromValues(size, -size * 3)];
        pointsRotated = [vec2.create(), vec2.create()];
        pointsFinal = [vec2.create(), vec2.create()];

        // Apply the rotation matrix to the point
        for (let i = 0; i < pointsOriginal.length; i++) {
            vec2.transformMat2(pointsRotated[i], pointsOriginal[i], rotationMatrix);
            pointsFinal[i] = vec2.fromValues(start[0] + pointsRotated[i][0], start[1] + pointsRotated[i][1]);
        }

        path += `M${pointsFinal[0][0]},${pointsFinal[0][1]}`;
        path += ` L${pointsFinal[1][0]},${pointsFinal[1][1]}`;


    }
    
    return path;

}



function getSpringPath(spring) {
    // draw shape representing a spring

    // voor verplaatsing + rotatie
    const { mat2, vec2 } = glMatrix;
    let size = 0.1;

    const origin = vec2.fromValues(0, 0);
    const p1 = vec2.fromValues(0, -1.0 * size);
    const p2 = vec2.fromValues(-1.5 * size, -2.0 * size);
    const p3 = vec2.fromValues(1.5 * size, -2.0 * size);
    const p4 = vec2.fromValues(0, -3 * size);
    const p5 = vec2.fromValues(0, -4 * size);
    const p6 = vec2.fromValues(-1 * size, -4 * size);
    const p7 = vec2.fromValues(1 * size, -4 * size);

    const start = vec2.fromValues(spring.node.x, spring.node.z);
    let theta = 0;

    let originalPoints = [origin, p1, p2, p3, p4, p5, p6, p7];
    let rotatedPoints = [vec2.create(), vec2.create(), vec2.create(), vec2.create(), vec2.create(), vec2.create(), vec2.create(), vec2.create()];
    let translatedPoints = [vec2.create(), vec2.create(), vec2.create(), vec2.create(), vec2.create(), vec2.create(), vec2.create(), vec2.create()];

    // Create a 2D rotation matrix
    const rotationMatrix = mat2.fromRotation(mat2.create(), theta);

    // Apply the rotation matrix to the point
    for (let i = 0; i < 8; i++) {
        vec2.transformMat2(rotatedPoints[i], originalPoints[i], rotationMatrix);
        translatedPoints[i] = vec2.fromValues(start[0] + rotatedPoints[i][0], start[1] + rotatedPoints[i][1]);
    }

    let path = `M${translatedPoints[0][0]},${translatedPoints[0][1]}`;
    for (let i = 1; i < 6; i++) {
        path += ` L${translatedPoints[i][0]},${translatedPoints[i][1]}`;
    }
    path += ` M${translatedPoints[6][0]},${translatedPoints[6][1]}`;
    path += ` L${translatedPoints[7][0]},${translatedPoints[7][1]}`;
   
    


    return path;


}
  


function getMemberShapes(model, display) {


    const memberShapes = model.members.map(member => ({
        type: 'line',
        x0: member.startNode.x,
        y0: member.startNode.z,
        x1: member.endNode.x,
        y1: member.endNode.z,
        line: { color: memberColor, width: memberWidth },
        opacity: 0.5,
        label: {
            //text: String(member.id) + '(' + String(member.section.id) + ')',
            text: String(member.id),
            font: memberFont,
            textposition: 'top center',


        }
    }));
    return memberShapes;
}
function getMembersInfo(model) {

 

    const memberInfo = model.members.map(m => ({
        x: [m.midPointF.x],
        y: [m.midPointF.y],
        mode: 'markers',
        text: m.number + "." + m.section.number,
        textangle: 180,
        showarrow: false,
        textposition: 'top',
        type: 'scatter',
       
       


    }));
    return memberInfo;
}


function getSupportsWithText(model, color, size) {

    const angles = model.supports.map(item => item.rotation);
    const markerArray = model.supports.map(item => ({
        angle: item.rotation,
        symbol: item.symbol,
        size: size,
        color: color
    }));
    const trace = {
        mode: 'markers+text',
        type: 'scatter',
        //marker: markerArray,
        marker: {
            color: color,
            size: size,
            symbol: model.supports.map(item => item.symbol),
            angle: angles,
        },
        x: model.supports.map(item => item.node.x),
        y: model.supports.map(item => item.node.z),
        text: model.supports.map(item => item.id),

    }
    return trace;
}





function getArrowPath(p0, p1) {

    const { vec2 } = glMatrix;
    //const p0 = vec2.fromValues(x0, y0);
    //const p1 = vec2.fromValues(x1, y1);
    const dist = vec2.distance(p1, p0);
    const headSize = 0.125 * dist;

    const direction = vec2.create();
    vec2.subtract(direction, p1, p0);
    
    // Calculate the angle using atan2
    let theta = Math.atan2(direction[1], direction[0]);
    const headAngle1 = theta + Math.PI / 6; // Angle for one side of the triangle
    const headAngle2 = theta - Math.PI / 6; // Angle for the other side of the triangle
    const p3 = vec2.fromValues(p1[0] - headSize * Math.cos(headAngle1), p1[1] - headSize * Math.sin(headAngle1));
    const p4 = vec2.fromValues(p1[0] - headSize * Math.cos(headAngle2), p1[1] - headSize * Math.sin(headAngle2));

    let path = `M${p0[0]}, ${p0[1]}`;// move naar p0
    path += ` L${p1[0]}, ${p1[1]}`; // streep

    path += ` M${p3[0]}, ${p3[1]}`; // driehoek
    path += ` L${p1[0]}, ${p1[1]}`;
    path += ` L${p4[0]}, ${p4[1]}`;
    path += ` L${p3[0]}, ${p3[1]}`;
   
    return path;


}


function getPointLoad(memberForce, forceScale) {

}

function getLijnlast(memberForce, forceScale) {
    const a = memberForce.distanceA;
    const member = memberForce.member;
    let L = memberForce.member.length; // NB. kan ook negatief zijn.
    const { mat2, vec2 } = glMatrix;
    const pMember1 = vec2.fromValues(member.startNode.x, member.startNode.z);
    const pMember2 = vec2.fromValues(member.endNode.x, member.endNode.z);

    const direction = vec2.create();
    vec2.subtract(direction, pMember2, pMember1);
    // Calculate the angle using atan2
    let theta = Math.atan2(direction[1], direction[0]);
    let thetaX = Math.PI / 2 - theta; // used for skew in global-x 

    // Create a 2D rotation matrix
    let transformationMatrix = mat2.fromRotation(mat2.create(), theta);

    let dy = 0; // used for projection-Z
    let dx = 0; // used for projection-X
    let flip = false; // used for projection-X en global-x

    let skewX = 0;
    let skewY = 0;

  

    //const vMember = vec2.fromRotation(angle);

    //let vForce = vec2.fromValues(0, 1); // default z

    switch (memberForce.type) {
        case 101: // proj.X
            L = memberForce.member.dz;
            theta = Math.PI / 2; // 90 graden 
            transformationMatrix = mat2.fromRotation(mat2.create(), theta);
            //vForce = vec2.fromValues(1, 0) // x
            flip = true;
            dx = Math.min(0, memberForce.member.dx);
            break; // proj.x
        case 103: // proj.Z
            L = memberForce.member.dx;
            theta = 0; // horizontaal 0 graden
            transformationMatrix = mat2.fromRotation(mat2.create(), theta);
            //vForce = vec2.fromValues(0, 1) // z
            dy = Math.max(0, memberForce.member.dz);
            break; // proj.z

        case 201: // GLOBAAL.X
            L = memberForce.member.dz; // de lengte is globaal
            flip = true;
            skewX = Math.tan(thetaX); // de hoek zal aangepast moeten worden
            // eerst draaien
            const rotMat = mat2.fromRotation(mat2.create(), Math.PI / 2);
            // dan skew
            const skewMat = mat2.fromValues(1, skewY, skewX, 1); 

            // omdat we eerst gedraaid zijn (rotatieMatrix) en daarna skewen moeten
            // we de matrix vermenigvuldigen, maar in de omgekeerde volgorde
            // dus skewMatrix * rotatieMatrix
            mat2.multiply(transformationMatrix, skewMat, rotMat);

            

           


            break; // glob.x

        case 203: // GLOBAAL.Z
            L = memberForce.member.dx; // de lengte is globaal
            skewY = Math.tan(theta);
            // create a vertical skew matrix
            transformationMatrix = mat2.fromValues(1, skewY, skewX, 1);
            break; // glob.z

        //case 301: L = mem
    }

   




    


    const b = memberForce.distanceB;

    // neem de absolute waarde over, we tekenen altijd absoluut
    // en voegen een pijl toe voor de juiste richting
    const dh = Math.abs(memberForce.endMagnitude) - Math.abs(memberForce.startMagnitude);
    const h1 = Math.abs(memberForce.startMagnitude * forceScale) ;
    const h2 = Math.abs(memberForce.endMagnitude * forceScale);

    const x0 = dx + memberForce.member.startNode.x;
    const y0 = dy + memberForce.member.startNode.z;

    //const x1 = memberForce.member.endNode.x;
    //const y1 = dy + memberForce.member.endNode.z;



    





    // Define the original point
    //const point = vec2.fromValues(1, 0);


    //
    let gA = a;
    let gB = L - b;


    // tweeks
    if (L < 0) {
        gA = -a;
        gB = L + b;
    }


    //let xa = x0 + a;
    //let xb = x0 + (L - b);
    //let ya = y0 + h1;
    //let yb = y0 + h2;

    //if (x0 > x1) {
    //    xa = x0 - a;
    //    xb = x0 + (L + b);
    //    gA = 0 - a;
    //    gB = 0 + (L + b);
    //}
    

   

    // Define the original points
    //let p1 = vec2.fromValues(xa, y0 + 0); // start
    //let p2 = vec2.fromValues(xa, ya); // start+magnitude
    //let p3 = vec2.fromValues(xb, yb); // end+magnitude
    //let p4 = vec2.fromValues(xb, y0 + 0); // end

    // Create a vector to store the rotated point
    const rp1 = vec2.create();
    const rp2 = vec2.create();
    const rp3 = vec2.create();
    const rp4 = vec2.create();

    //const rp5 = vec2.create();
    //const rp6 = vec2.create();



    // originele punten tov global assen
    let p1 = vec2.fromValues(gA, 0); // start
    let p2 = vec2.fromValues(gA, h1); // start+magnitude
    let p3 = vec2.fromValues(gB, h2); // end+magnitude
    let p4 = vec2.fromValues(gB, 0); // end

    //let p5 = vec2.fromValues((gA + gB) * 0.5, 0);
    //let p6 = vec2.fromValues((gA + gB) * 0.5, (h1 + h2) * 0.5);


    // roteer

    // Apply the rotation matrix to the point
    vec2.transformMat2(rp1, p1, transformationMatrix);
    vec2.transformMat2(rp2, p2, transformationMatrix);
    vec2.transformMat2(rp3, p3, transformationMatrix);
    vec2.transformMat2(rp4, p4, transformationMatrix);

    //vec2.transformMat2(rp5, p5, rotationMatrix);
    //vec2.transformMat2(rp6, p6, rotationMatrix);



    // verplaats

    const p0 = vec2.fromValues(x0, y0);


    const fp1 = vec2.fromValues(p0[0] + rp1[0], p0[1] + rp1[1]);
    const fp2 = vec2.fromValues(p0[0] + rp2[0], p0[1] + rp2[1]);
    const fp3 = vec2.fromValues(p0[0] + rp3[0], p0[1] + rp3[1]);
    const fp4 = vec2.fromValues(p0[0] + rp4[0], p0[1] + rp4[1]);

    //const fp5 = vec2.fromValues(p0[0] + rp5[0], p0[1] + rp5[1]);
    //const fp6 = vec2.fromValues(p0[0] + rp6[0], p0[1] + rp6[1]);
  
    let points = [fp1, fp2, fp3, fp4];
    let points2 = [];
    let points3 = [];

    // pijlen
    // 
    let isNegative = (memberForce.startMagnitude + memberForce.endMagnitude) / 2 < 0;

    for (let i = 1; i < 3; i++) {

        let startY = 0;
        let eindY = 0;
        //let tipY = 0;
        let posX = i / 3.0 * (gB - gA) + gA;
        let hx = h1 + (dh * i / 3.0) * forceScale;

        if (
            (isNegative && flip == false) ||
            (isNegative == false && flip)
        ) {
            startY = hx;
            eindY = 0;
            //tipY = 0 + hx * 0.1;
        }
        else {
            startY = 0;
            eindY = hx;
            //tipY = eindY - hx * 0.1;
        }


        let pijl1 = vec2.fromValues(posX, startY); 
        let pijl2 = vec2.fromValues(posX, eindY); 
        //let pijl3 = vec2.fromValues(posX - hx * 0.1, tipY);
        //let pijl4 = vec2.fromValues(posX + hx * 0.1, tipY); 
        const pijl11 = vec2.create();
        const pijl12 = vec2.create();
        //const pijl13 = vec2.create();
        //const pijl14 = vec2.create();
        vec2.transformMat2(pijl11, pijl1, transformationMatrix);
        vec2.transformMat2(pijl12, pijl2, transformationMatrix);
        //vec2.transformMat2(pijl13, pijl3, transformationMatrix);
        //vec2.transformMat2(pijl14, pijl4, transformationMatrix);
        const pijl21 = vec2.fromValues(p0[0] + pijl11[0], p0[1] + pijl11[1]);
        const pijl22 = vec2.fromValues(p0[0] + pijl12[0], p0[1] + pijl12[1]);
        //const pijl23 = vec2.fromValues(p0[0] + pijl13[0], p0[1] + pijl13[1]);
        //const pijl24 = vec2.fromValues(p0[0] + pijl14[0], p0[1] + pijl14[1]);

        if (i == 1) {
            points2 = [pijl21, pijl22];
        }
        else if (i == 2) {
            points3 = [pijl21, pijl22];
        }
       

        

    }


    points.concat[points2];
    points.concat[points3];


    
    return points.concat(points2, points3);

}

{
    // globale variabelen
    
}


function getLijnlasten(geometry, memberForces) {

    if (memberForces.length == 0) {
        return [];
    }
    // bepaal de force-scale
    // de grootste absolute magnitude wordt op 1 meter getekend.
    const absStartMagnitudes = memberForces.map(f => Math.abs(f.startMagnitude));
    const absEndMagnitudes = memberForces.map(f => Math.abs(f.endMagnitude));
    const maxMagnitudeAbs = Math.max(Math.max(...absStartMagnitudes), Math.max(...absEndMagnitudes));
    
    // de schaal gelijk aan 1 meter / magnitude 
    ForceScale = 1.0 / maxMagnitudeAbs;
    const newArray = memberForces.map(createMemberForce);
    return newArray;
}


function createMemberForcePointLoad(memberForce) {
    const points = getPointLoad(memberForce, ForceScale);

}

function createMemberForceDistributedLoad(memberForce) {
    const points = getLijnlast(memberForce, ForceScale);
    const tekst = Math.max(Math.abs(memberForce.startMagnitude), Math.abs(memberForce.endMagnitude)).toFixed(2);

    let i = 0;
    const p1 = `${points[i][0]},${points[i++][1]}`;
    const p2 = `${points[i][0]},${points[i++][1]}`;
    const p3 = `${points[i][0]},${points[i++][1]}`;
    const p4 = `${points[i][0]},${points[i++][1]}`;

    let path = `M${p1} L${p2} L${p3} L${p4} L${p1}`;

    path += getArrowPath(points[4], points[5]);
    path += getArrowPath(points[6], points[7]);

    const shape = {
        type: 'path',
        path: path,
        //fillcolor: 'rgba(44, 160, 101, 0.5)',
        line: {
            color: 'black',
            width: 1
        },
        label: {
            text: tekst,
            font: { size: 10, color: 'black' },
            textposition: 'inside',
        },
    }
    return shape;
}

function createMemberForce(memberForce) {

    switch (memberForce.type) {
        case 1:
        case 2:
        case 3:
        case 101:
        case 102:
        case 103:
        case 201:
        case 202:
        case 203:
        case 301:
        case 302:
        case 303:   
            return createMemberForceDistributedLoad(memberForce);
            break;
        case 401:
        case 402:
        case 403:
        case 501:
        case 502:
        case 503:
        case 601:
        case 602:
        case 603:
            

            break;
    }

    
}



function getMemberForceTest(memberForce) {

    const { mat2, vec2 } = glMatrix;
    // verplaats eerst naar startNode
    const theta = Math.PI / 2;
    const rotationMatrix = mat2.fromRotation(mat2.create(), theta);
    // Define the original point
    const point = vec2.fromValues(1, 0);

    // Create a vector to store the rotated point
    const rotatedPoint = vec2.create();

    // Apply the rotation matrix to the point
    vec2.transformMat2(rotatedPoint, point, rotationMatrix);



}



