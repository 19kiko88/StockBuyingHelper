import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VtiQueryComponent } from './vti-query.component';

describe('VtiQueryComponent', () => {
  let component: VtiQueryComponent;
  let fixture: ComponentFixture<VtiQueryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VtiQueryComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(VtiQueryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
