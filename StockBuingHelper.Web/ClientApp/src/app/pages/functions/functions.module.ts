import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SharedModule } from '../../shared/shared.module';
import { VtiQueryComponent } from './components/vti-query/vti-query.component';
import { HistoryComponent } from './components/history/history.component';

@NgModule({
  declarations: [
    VtiQueryComponent,
    HistoryComponent
  ],
  imports: [
    CommonModule,    
    FormsModule, 
    ReactiveFormsModule,
    SharedModule
  ],
  exports:[
    VtiQueryComponent,
    HistoryComponent
  ]
})
export class FunctionsModule { }
