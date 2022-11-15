import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { MagicCircleComponent } from "./magic-circle.component";
import { RingComponent } from './ring/ring.component';
import { SquareComponent } from './square/square.component';

const EXPORTS = [
    MagicCircleComponent,
    RingComponent,
    SquareComponent
];

@NgModule({
    declarations: [ ...EXPORTS  ],
    exports: [ ...EXPORTS ],
    imports: [
        CommonModule,
        ReactiveFormsModule,
        FormsModule
    ],
    providers: [ ]
})
export class MagicCircleModule { }