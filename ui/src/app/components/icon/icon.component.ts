import { Component, Input } from '@angular/core';

@Component({
    selector: 'cba-icon',
    templateUrl: './icon.component.html',
    styleUrls: ['./icon.component.scss']
})
export class IconComponent { 
    @Input('font-size') size?: string; 
    @Input('unsize') unsize: boolean = false;
}
