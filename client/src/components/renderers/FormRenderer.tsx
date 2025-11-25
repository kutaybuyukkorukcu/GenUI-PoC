import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '../ui/card';

import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Send } from 'lucide-react';
import { useState } from 'react';

interface FormField {
  name: string;
  label: string;
  type: 'text' | 'number' | 'email' | 'date' | 'select' | 'textarea';
  placeholder?: string;
  required?: boolean;
  options?: string[]; // For select fields
  defaultValue?: string | number;
}

interface FormRendererProps {
  title: string;
  description?: string;
  fields: FormField[];
  submitText?: string;
  onSubmit?: (data: Record<string, string | number>) => void;
  sendMessage?: (message: string) => void;
}

export const FormRenderer = ({
  title,
  description,
  fields,
  submitText = 'Submit',
  onSubmit,
  sendMessage,
}: FormRendererProps) => {
  const [formData, setFormData] = useState<Record<string, string | number>>(() => {
    const initialData: Record<string, string | number> = {};
    fields.forEach((field) => {
      if (field.defaultValue !== undefined) {
        initialData[field.name] = field.defaultValue;
      }
    });
    return initialData;
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    // If sendMessage is provided, send the form data to backend
    if (sendMessage) {
      const formDataJson = JSON.stringify(formData);
      sendMessage(`FORM_SUBMIT:${formDataJson}`);
    }
    
    // Also call onSubmit if provided (for backwards compatibility)
    onSubmit?.(formData);
  };

  const handleChange = (name: string, value: string | number) => {
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  return (
    <Card className="w-full">
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        {description && <p className="text-sm text-muted-foreground">{description}</p>}
      </CardHeader>
      <form onSubmit={handleSubmit}>
        <CardContent className="space-y-4">
          {fields.map((field) => (
            <div key={field.name} className="space-y-2">
              <label htmlFor={field.name} className="text-sm font-medium">
                {field.label}
                {field.required && <span className="text-red-500 ml-1">*</span>}
              </label>

              {field.type === 'select' && field.options ? (
                <select
                  id={field.name}
                  name={field.name}
                  required={field.required}
                  value={formData[field.name] || ''}
                  onChange={(e) => handleChange(field.name, e.target.value)}
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  <option value="">Select an option...</option>
                  {field.options.map((option) => (
                    <option key={option} value={option}>
                      {option}
                    </option>
                  ))}
                </select>
              ) : field.type === 'textarea' ? (
                <textarea
                  id={field.name}
                  name={field.name}
                  placeholder={field.placeholder}
                  required={field.required}
                  value={formData[field.name] || ''}
                  onChange={(e) => handleChange(field.name, e.target.value)}
                  rows={4}
                  className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                />
              ) : (
                <Input
                  id={field.name}
                  name={field.name}
                  type={field.type}
                  placeholder={field.placeholder}
                  required={field.required}
                  value={formData[field.name] || ''}
                  onChange={(e) =>
                    handleChange(
                      field.name,
                      field.type === 'number' ? Number(e.target.value) : e.target.value
                    )
                  }
                />
              )}
            </div>
          ))}
        </CardContent>
        <CardFooter>
          <Button type="submit" className="w-full">
            <Send className="mr-2 h-4 w-4" />
            {submitText}
          </Button>
        </CardFooter>
      </form>
    </Card>
  );
};
