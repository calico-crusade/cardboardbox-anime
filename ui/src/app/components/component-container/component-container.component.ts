import { Component, Input, OnInit } from '@angular/core';

@Component({
    selector: 'cba-container',
    templateUrl: './component-container.component.html',
    styleUrls: ['./component-container.component.scss']
})
export class ComponentContainerComponent implements OnInit {

    @Input() loading: boolean = false;
    @Input() error?: string;

    @Input('handle-scroll') handleScroll: boolean = false;

    constructor() { }

    ngOnInit(): void {

    }

}
